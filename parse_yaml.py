import sys, re

def get_component_data(file_path):
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Simple regex to find BoxCollider sizes and centers
    colliders = re.findall(r'BoxCollider:.*?m_Size: {x: ([-\d.]+), y: ([-\d.]+), z: ([-\d.]+)}', content, re.DOTALL)
    for c in colliders:
        print(f"File: {file_path} BoxCollider Size: {c}")

    transforms = re.findall(r'Transform:.*?m_LocalPosition: {x: ([-\d.]+), y: ([-\d.]+), z: ([-\d.]+)}', content, re.DOTALL)
    if transforms:
        print(f"File: {file_path} Transform Pos: {transforms[0]}")

for path in sys.argv[1:]:
    get_component_data(path)
