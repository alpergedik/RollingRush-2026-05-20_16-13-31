import re

with open('Assets/Scenes/GameScene.unity', 'r', encoding='utf-8') as f:
    content = f.read()

# Find GameObjects
game_objects = re.findall(r'--- !u!1 &(\d+)\nGameObject:\n.*?m_Name: (.*?)\n', content, re.DOTALL)
go_dict = {match[0]: match[1] for match in game_objects}

# Find Transforms
transforms = re.findall(r'--- !u!4 &(\d+)\nTransform:\n.*?m_GameObject: {fileID: (\d+)}.*?m_LocalPosition: {x: ([-\d.]+), y: ([-\d.]+), z: ([-\d.]+)}', content, re.DOTALL)

for t in transforms:
    go_id = t[1]
    if go_id in go_dict:
        name = go_dict[go_id]
        if name in ['Player', 'Player Pivot', 'Wheel']:
            print(f"{name} position: X={t[2]}, Y={t[3]}, Z={t[4]}")
