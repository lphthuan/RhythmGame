$path = "d:\GameOnline\RhythmGame\Assets\_Game\Scenes\Sandbox\tndKhoa\KhoaCuBu.unity"
$lines = Get-Content $path

$sceneRootsIdx = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match "^SceneRoots:") {
        $sceneRootsIdx = $i
        break
    }
}

if ($sceneRootsIdx -eq -1) {
    Write-Host "Could not find SceneRoots!"
    exit 1
}

$insertIdx = -1
for ($i = $sceneRootsIdx + 1; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match "^--- !u!" -or $lines[$i].Trim() -eq "") {
        $insertIdx = $i
        break
    }
}

if ($insertIdx -eq -1) {
    $insertIdx = $lines.Count
}

$yamlToAppend = @"

--- !u!1 &800000001
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 800000003}
  - component: {fileID: 800000002}
  m_Layer: 0
  m_Name: EarlyLateIndicator
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!114 &800000002
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 800000001}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ba591ee4aafd2d54cbec654ef24aa12e, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  _noteManager: {fileID: 0}
  _targetCanvas: {fileID: 0}
  _posYMiddle: 30
  _posYTop: 200
  _posYBottom: -80
  _posX: 0
  _earlyColor: {r: 0.43, g: 0.78, b: 1, a: 1}
  _lateColor: {r: 1, g: 0.7, b: 0.28, a: 1}
  _fontSize: 28
  _fontStyle: 1
  _startScale: 0.55
  _popScale: 1.15
  _popDuration: 0.1
  _lifetime: 0.55
  _fadeStartTime: 0.28
  _floatUpDistance: 18
--- !u!4 &800000003
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 800000001}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
"@

$newLines = New-Object System.Collections.Generic.List[string]
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($i -eq $insertIdx) {
        $newLines.Add("  - {fileID: 800000003}")
    }
    $newLines.Add($lines[$i])
}

if ($insertIdx -eq $lines.Count) {
    $newLines.Add("  - {fileID: 800000003}")
}

$newLines.Add($yamlToAppend)

[System.IO.File]::WriteAllLines($path, $newLines)
Write-Host "Done"