# Plano de correção dos terminais do Thoth

## Objetivo

Corrigir o comportamento dos terminais embutidos do Thoth, com foco inicial no problema de não conseguir rolar para cima e ver o histórico na aba `Terminais` da rota de prompt e no drawer `Terminal do agente`, sem regredir suporte a CLIs interativas, zoom, múltiplas sessões, reconexão via SignalR e histórico carregado do backend.

Este plano foi escrito a partir da leitura do código atual, não de uma execução local do app. A prioridade é transformar o bug de scroll/histórico em um conjunto de correções pequenas, testáveis e reutilizáveis em todas as superfícies de terminal.

## Superfícies afetadas

- Rota de prompt: `frontend/src/features/prompts/terminals-panel.tsx`.
- Drawer de prompt filho/agente: `frontend/src/features/terminals/agent-terminal-drawer.tsx`.
- Página global de terminais: `frontend/src/features/terminals/terminals-page.tsx` e `frontend/src/features/terminals/terminal-card.tsx`.
- Componente xterm compartilhado: `frontend/src/features/prompts/terminal-view.tsx` e `frontend/src/features/prompts/terminal-view.css`.
- Backend de sessão, buffer, offsets e histórico: `backend/src/Thoth.Infrastructure/Terminals/TerminalSessionManager.cs`, `TerminalSession.cs`, `TerminalOptions.cs` e `TerminalOutputHistoryDto.cs`.

## Diagnóstico resumido

1. O app já usa `xterm.js` no `TerminalView` com `scrollback: 10_000`, carrega histórico pelo endpoint `GET /api/terminals/{sessionId}/output-history` e recebe chunks em tempo real via SignalR.
2. O histórico do backend é em memória por sessão, usa offsets (`StartOffset`, `EndOffset`) e tem limite padrão de `4 MB` via `MaxOutputHistoryBytes`.
3. A rota de prompt renderiza o terminal dentro de um container com altura fixa `h-[min(70vh,640px)]` e `overflow-hidden`.
4. O drawer de agente coloca o `TerminalsPanel` dentro de um container `overflow-auto`, criando uma competição de scroll entre o drawer/página e o viewport interno do xterm.
5. O `TerminalView` só chama `preventDefault`/`stopPropagation` quando `Ctrl`/`Meta` + scroll é usado para zoom. O scroll normal pode borbulhar para containers externos.
6. O cliente faz deduplicação por offset, mas não trata explicitamente lacunas quando `startOffset > outputEndOffset`. Se um chunk for perdido em reconexão ou atraso, o terminal pode pular bytes sem avisar o usuário.
7. O backend já informa `IsTruncated`, mas a UI não parece transformar isso em feedback visível de “histórico truncado”.

## Hipótese principal para o bug de scroll

O bug mais provável não está na ausência de scrollback, porque o `TerminalView` já configura `scrollback: 10_000`. O problema parece estar na combinação de:

- containers externos com `overflow-hidden` ou `overflow-auto`;
- drawer com scroll próprio envolvendo todo o painel;
- ausência de isolamento de scroll/overscroll no wrapper do xterm;
- falta de controles auxiliares para navegar o buffer quando o scroll do navegador compete com o viewport do terminal.

## Plano de correção

### 1. Criar um frame único para terminais

Criar um componente compartilhado, por exemplo:

```text
frontend/src/features/prompts/terminal-frame.tsx
```

Responsabilidades:

- centralizar altura, borda, background, `min-h-0`, `overflow-hidden` e isolamento visual;
- expor variantes de layout: `prompt`, `drawer`, `card`, `global`;
- garantir que o terminal tenha um único dono de tamanho em cada superfície;
- remover duplicação entre `TerminalsPanel` e `TerminalCard`;
- evitar que o drawer/página seja o elemento que captura a rolagem do terminal.

Uso esperado:

- Em `TerminalsPanel`, substituir o container direto do `TerminalView` pelo `TerminalFrame`.
- Em `TerminalCard`, trocar o container expandido pelo mesmo frame na variante `card`.
- Em `AgentTerminalDrawer`, permitir que o corpo do drawer seja `min-h-0` e que o `TerminalsPanel` ocupe a altura disponível, sem `overflow-auto` competindo com o xterm.

### 2. Isolar scroll no `TerminalView`

Atualizar `terminal-view.css` para conter o overscroll no terminal:

```css
.thoth-terminal,
.thoth-terminal .xterm,
.thoth-terminal .xterm-viewport {
  overscroll-behavior: contain;
}

.thoth-terminal .xterm-viewport {
  scrollbar-gutter: stable;
  scrollbar-width: thin;
}
```

Atualizar o listener de `wheel` em `terminal-view.tsx`:

- manter `preventDefault` apenas para `Ctrl`/`Meta` + scroll, porque isso é zoom;
- para scroll normal, chamar `stopPropagation()` sem `preventDefault()`, deixando o xterm processar o scroll, mas impedindo que a página/drawer “roube” a rolagem;
- confirmar com teste manual em mouse, trackpad e scrollbar.

Pseudocódigo:

```ts
const onWheel = (event: WheelEvent) => {
  if (event.ctrlKey || event.metaKey) {
    if (!activeRef.current || !onZoomRef.current) return
    event.preventDefault()
    event.stopPropagation()
    onZoomRef.current(event.deltaY < 0 ? 1 : -1)
    return
  }

  event.stopPropagation()
}
```

### 3. Preservar posição quando o usuário rolar para cima

Implementar uma política explícita de “auto-follow”:

- se o usuário está no fim do terminal, novas saídas mantêm o terminal no fim;
- se o usuário rolou para cima, novas saídas não puxam a tela para baixo;
- mostrar um pequeno botão/flutuante “Novas saídas — ir ao fim” quando houver output novo e o usuário estiver fora do fim;
- adicionar ações `Ir ao topo` e `Ir ao fim` no frame ou toolbar do terminal.

Sugestão técnica:

- ouvir o scroll do `.xterm-viewport`;
- calcular `isAtBottom` usando o viewport/base do buffer do xterm;
- depois de `term.write(...)`, chamar `term.scrollToBottom()` somente quando `isAtBottom` estava verdadeiro antes da escrita.

### 4. Tratar lacunas de output por offset

Refatorar a lógica de escrita de output do `TerminalView` para lidar com quatro casos:

1. chunk duplicado: ignorar;
2. chunk sobreposto: escrever só a parte nova;
3. chunk contíguo: escrever normal;
4. lacuna (`startOffset > outputEndOffset`): pausar escrita, refazer `getTerminalOutputHistory(sessionId)` e tentar resincronizar.

Comportamento esperado em lacuna:

- se o histórico retornado ainda cobre o offset esperado, preencher a lacuna e continuar;
- se o histórico já foi truncado antes do offset esperado, limpar/recriar o buffer visual ou escrever uma mensagem discreta de aviso, carregar o histórico disponível e mostrar badge `Histórico truncado`;
- nunca avançar `outputEndOffset` como se a lacuna não existisse.

### 5. Exibir truncamento e estado de conexão

Usar `IsTruncated` do `TerminalOutputHistoryDto` para feedback visível:

- badge pequeno `Histórico truncado` na toolbar;
- tooltip explicando que o backend mantém apenas os últimos bytes configurados;
- opcionalmente, mostrar `StartOffset`/`EndOffset` em modo debug.

Também exibir estado de conexão:

- `Conectado`;
- `Reconectando`;
- `Sessão encerrada`;
- `Histórico indisponível` quando o endpoint retornar `404`.

### 6. Ajustar o drawer de agente

No `AgentTerminalDrawer`:

- substituir o corpo atual `min-h-0 overflow-auto px-4 py-3` por uma estrutura onde apenas listas/headers externos possam rolar, mas o terminal receba altura clara;
- evitar que o scroll do drawer seja usado quando o cursor está sobre o terminal;
- se houver muitas abas/sessões, mover o scroll para a região de abas, não para o viewport inteiro do drawer;
- testar o mesmo comportamento em tela pequena e em `96vw / 72rem`.

### 7. Ajustar a página global `/terminais`

Na página global, o `TerminalCard` usa altura menor (`min(50vh,420px)`). Aplicar o mesmo `TerminalFrame` e isolamento de scroll, mas manter a experiência de card:

- `Visualizar` abre o terminal sem quebrar o grid;
- scroll interno do xterm funciona;
- scroll da página continua funcionando quando o mouse está fora do terminal;
- fechar/ocultar/reabrir recarrega histórico corretamente.

### 8. Validar backend e configuração de histórico

O backend já tem as peças principais, mas vale reforçar:

- teste de truncamento com `MaxOutputHistoryBytes` pequeno;
- teste de offsets em `TerminalSessionManager` para garantir `StartOffset`, `EndOffset` e `IsTruncated` consistentes;
- documentar no README ou em config que o histórico é em memória, por sessão, e limitado por `MaxOutputHistoryBytes`;
- avaliar se `4 MB` é suficiente para agentes longos. Se não for, aumentar padrão para `8 MB` ou tornar mais visível em `appsettings`.

### 9. Testes automatizados sugeridos

Frontend:

- `terminal-output-buffer.test.ts`: extrair a lógica de dedupe/overlap/gap para função pura e testar os quatro casos de offset.
- `terminal-scroll-behavior.test.tsx`: validar que `Ctrl+wheel` aplica zoom e que wheel normal não propaga para o container pai.
- `terminal-frame.test.tsx`: garantir classes de `min-h-0`, isolamento e variantes de altura.
- `agent-terminal-drawer.test.tsx`: garantir que o drawer hospeda o painel sem `overflow-auto` competindo com o terminal.
- `terminals-panel.test.tsx`: garantir que alternar abas preserva instâncias e session id ativo.

Backend:

- `TerminalSessionManagerTests`: cobrir truncamento, retorno de histórico e remoção de sessões.
- Teste de opção `MaxOutputHistoryBytes = 0` para confirmar comportamento sem histórico.
- Teste de saída maior que o limite para confirmar `IsTruncated = true` e `StartOffset > 0`.

### 10. Roteiro manual de QA

Executar os cenários abaixo na rota de prompt, no drawer de agente e em `/terminais`:

```powershell
1..1000 | ForEach-Object { "linha $_"; Start-Sleep -Milliseconds 2 }
```

Checklist:

- scroll do mouse/trackpad sobe até linhas antigas;
- scrollbar do terminal aparece e funciona;
- a página/drawer não rola quando o cursor está sobre o terminal;
- fora do terminal, a página/drawer ainda rola normalmente;
- `Ctrl+scroll` continua alterando zoom;
- novas saídas não puxam a tela para baixo quando o usuário está lendo histórico;
- botão `Ir ao fim` aparece quando há saída nova fora do fim;
- alternar sessão não perde output;
- ocultar/reabrir card em `/terminais` recarrega histórico;
- fechar drawer e abrir de novo recarrega histórico da sessão ainda viva;
- reconexão SignalR não duplica nem perde output visível;
- quando o histórico for truncado, a UI informa isso claramente.

## Ordem recomendada de implementação

1. Escrever testes unitários da lógica de offset antes de mexer em `TerminalView`.
2. Criar `TerminalFrame` e aplicar na rota de prompt.
3. Corrigir isolamento de wheel/overscroll no `TerminalView`.
4. Aplicar o mesmo frame no drawer de agente.
5. Aplicar o mesmo frame na página global `/terminais`.
6. Adicionar política de auto-follow e botão `Ir ao fim`.
7. Adicionar tratamento de lacunas de offset e resincronização via histórico.
8. Exibir `Histórico truncado` e estados de conexão.
9. Reforçar testes backend de histórico/truncamento.
10. Rodar QA manual nos três contextos.

## Critérios de aceite

- O usuário consegue rolar para cima e ler histórico na rota de prompt.
- O usuário consegue rolar para cima e ler histórico no drawer de prompt filho/agente.
- O comportamento também funciona em `/terminais` e nos terminais genéricos, se aplicável.
- `Ctrl+scroll` continua funcionando como zoom.
- Scroll normal do terminal não movimenta a página/drawer enquanto o cursor está sobre o terminal.
- O terminal não força o usuário para o fim quando ele está lendo histórico antigo.
- Perda/reconexão de SignalR não cria buracos silenciosos no output.
- Histórico truncado é mostrado explicitamente.
- Testes de frontend e backend passam.

## Arquivos prováveis de alteração

```text
frontend/src/features/prompts/terminal-view.tsx
frontend/src/features/prompts/terminal-view.css
frontend/src/features/prompts/terminal-frame.tsx
frontend/src/features/prompts/terminals-panel.tsx
frontend/src/features/terminals/agent-terminal-drawer.tsx
frontend/src/features/terminals/terminal-card.tsx
frontend/src/features/terminals/terminals-page.tsx
frontend/src/api/terminals.ts
frontend/src/api/schemas.ts
backend/src/Thoth.Infrastructure/Terminals/TerminalSessionManager.cs
backend/src/Thoth.Infrastructure/Terminals/TerminalOptions.cs
backend/tests/Thoth.Infrastructure.UnitTests/TerminalSessionManagerTests.cs
```

## Observações de risco

- Evitar `preventDefault()` no scroll normal, porque isso pode impedir o xterm de processar a rolagem.
- Não destruir/recriar instâncias do xterm em cada troca de aba; isso pode piorar perda de estado visual.
- Cuidado para não quebrar apps de tela cheia/alternate screen. Validar com comandos interativos e CLIs de agente.
- O histórico do backend é em memória. Se a API reiniciar, sessões e histórico somem; isso deve continuar documentado como comportamento esperado até existir persistência real de terminal.
