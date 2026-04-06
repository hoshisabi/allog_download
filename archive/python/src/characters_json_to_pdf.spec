# -*- mode: python ; coding: utf-8 -*-

a = Analysis(
    ['src/characters_json_to_pdf.py'],
    pathex=['f:/Users/decha/Documents/Projects/allog_download'],
    binaries=[],
    datas=[
        ('src/Roboto-Regular.ttf', '.'),
        ('src/Roboto-Bold.ttf', '.')
    ],
    hiddenimports=[],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    noarchive=False,
    optimize=0,
)
pyz = PYZ(a.pure)

exe = EXE(
    pyz,
    a.scripts,
    [],
    exclude_binaries=True,
    name='characters_json_to_pdf',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    console=True,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
)
coll = COLLECT(
    exe,
    a.binaries,
    a.datas,
    strip=False,
    upx=True,
    upx_exclude=[],
    name='characters_json_to_pdf',
)