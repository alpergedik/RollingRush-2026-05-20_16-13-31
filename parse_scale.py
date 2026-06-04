import re

with open('Assets/Prefabs/Obstacle_Block 1.prefab', 'r', encoding='utf-8') as f:
    content = f.read()

scales = re.findall(r'Transform:.*?\n.*?m_LocalScale: {x: ([-\d.]+), y: ([-\d.]+), z: ([-\d.]+)}', content, re.DOTALL)
print(f"Obstacle_Block 1 scales: {scales}")
