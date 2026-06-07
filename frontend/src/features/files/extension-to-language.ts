// Mapeia extensao de arquivo -> id de linguagem do Monaco.
// As linguagens abaixo ja vem registradas pelo `monaco-editor` carregado em
// `monaco-setup.ts`, entao basta o mapeamento (sem registrar linguagem nova).
// Projetos/configs .NET sao XML; `.sln` (formato proprio do MSBuild) cai em
// `ini`, que da o melhor destaque aproximado para `# comentarios` e `chave = valor`.
const EXTENSION_LANGUAGE_MAP: Record<string, string> = {
  axaml: 'xml',
  config: 'xml',
  cs: 'csharp',
  cshtml: 'razor',
  csproj: 'xml',
  css: 'css',
  csx: 'csharp',
  editorconfig: 'ini',
  fs: 'fsharp',
  fsi: 'fsharp',
  fsproj: 'xml',
  fsx: 'fsharp',
  go: 'go',
  html: 'html',
  htm: 'html',
  java: 'java',
  js: 'javascript',
  jsx: 'javascript',
  json: 'json',
  jsonc: 'json',
  manifest: 'xml',
  md: 'markdown',
  mjs: 'javascript',
  nuspec: 'xml',
  php: 'php',
  proj: 'xml',
  props: 'xml',
  ps1: 'powershell',
  psd1: 'powershell',
  psm1: 'powershell',
  py: 'python',
  razor: 'razor',
  rb: 'ruby',
  resx: 'xml',
  rs: 'rust',
  ruleset: 'xml',
  scss: 'scss',
  sh: 'shell',
  shproj: 'xml',
  sln: 'ini',
  slnx: 'xml',
  sql: 'sql',
  svg: 'xml',
  targets: 'xml',
  toml: 'ini',
  ts: 'typescript',
  tsx: 'typescript',
  vb: 'vb',
  vbhtml: 'razor',
  vbproj: 'xml',
  xaml: 'xml',
  xml: 'xml',
  yaml: 'yaml',
  yml: 'yaml',
}

export function extensionToLanguage(extension: string | null | undefined) {
  if (!extension) {
    return 'plaintext'
  }

  const normalized = extension.replace(/^\./, '').toLocaleLowerCase()
  return EXTENSION_LANGUAGE_MAP[normalized] ?? 'plaintext'
}
