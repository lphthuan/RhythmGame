import sys

file_path = r'd:\GameOnline\RhythmGame\Assets\_Game\Scenes\Sandbox\tndKhoa\KhoaCuBu.unity'

with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

scene_roots_idx = -1
for i, line in enumerate(lines):
    if line.startswith('SceneRoots:'):
        scene_roots_idx = i
        break

if scene_roots_idx == -1:
    print('Could not find SceneRoots!')
    sys.exit(1)

insert_idx = -1
for i in range(scene_roots_idx + 1, len(lines)):
    if lines[i].startswith('--- !u!') or lines[i].strip() == '':
        insert_idx = i
        break

if insert_idx == -1:
    insert_idx = len(lines)

game_object_id = '800000001'
component_id = '800000002'
transform_id = '800000003'

yaml_to_append = f'''--- !u!1 &{game_object_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {transform_id}}}
  - component: {{fileID: {component_id}}}
  m_Layer: 0
  m_Name: EarlyLateIndicator
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!114 &{component_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {game_object_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: ba591ee4aafd2d54cbec654ef24aa12e, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  _noteManager: {{fileID: 0}}
  _targetCanvas: {{fileID: 0}}
  _posYMiddle: 30
  _posYTop: 200
  _posYBottom: -80
  _posX: 0
  _earlyColor: {{r: 0.43, g: 0.78, b: 1, a: 1}}
  _lateColor: {{r: 1, g: 0.7, b: 0.28, a: 1}}
  _fontSize: 28
  _fontStyle: 1
  _startScale: 0.55
  _popScale: 1.15
  _popDuration: 0.1
  _lifetime: 0.55
  _fadeStartTime: 0.28
  _floatUpDistance: 18
--- !u!4 &{transform_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {game_object_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
'''

lines.insert(insert_idx, f'  - {{fileID: {transform_id}}}\n')
lines.append('\n' + yaml_to_append)

with open(file_path, 'w', encoding='utf-8') as f:
    f.writelines(lines)

print('Done')