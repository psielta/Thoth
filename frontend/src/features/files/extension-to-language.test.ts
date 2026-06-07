import { describe, expect, it } from 'vitest'
import { extensionToLanguage } from './extension-to-language'

describe('extensionToLanguage', () => {
  it('mapeia arquivos de projeto .NET para xml', () => {
    expect(extensionToLanguage('.csproj')).toBe('xml')
    expect(extensionToLanguage('.vbproj')).toBe('xml')
    expect(extensionToLanguage('.fsproj')).toBe('xml')
    expect(extensionToLanguage('.slnx')).toBe('xml')
    expect(extensionToLanguage('.props')).toBe('xml')
    expect(extensionToLanguage('.targets')).toBe('xml')
    expect(extensionToLanguage('.config')).toBe('xml')
    expect(extensionToLanguage('.resx')).toBe('xml')
    expect(extensionToLanguage('.xaml')).toBe('xml')
    expect(extensionToLanguage('.nuspec')).toBe('xml')
  })

  it('mapeia codigo .NET para a linguagem correspondente', () => {
    expect(extensionToLanguage('.cs')).toBe('csharp')
    expect(extensionToLanguage('.csx')).toBe('csharp')
    expect(extensionToLanguage('.fs')).toBe('fsharp')
    expect(extensionToLanguage('.vb')).toBe('vb')
    expect(extensionToLanguage('.razor')).toBe('razor')
    expect(extensionToLanguage('.cshtml')).toBe('razor')
    expect(extensionToLanguage('.ps1')).toBe('powershell')
  })

  it('usa ini para .sln e .editorconfig', () => {
    expect(extensionToLanguage('.sln')).toBe('ini')
    expect(extensionToLanguage('.editorconfig')).toBe('ini')
  })

  it('normaliza ponto inicial e caixa', () => {
    expect(extensionToLanguage('csproj')).toBe('xml')
    expect(extensionToLanguage('.CSPROJ')).toBe('xml')
    expect(extensionToLanguage('.Razor')).toBe('razor')
  })

  it('cai em plaintext para extensao desconhecida ou ausente', () => {
    expect(extensionToLanguage('.unknown')).toBe('plaintext')
    expect(extensionToLanguage(null)).toBe('plaintext')
    expect(extensionToLanguage(undefined)).toBe('plaintext')
    expect(extensionToLanguage('')).toBe('plaintext')
  })
})
