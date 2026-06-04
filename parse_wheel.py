import re

with open('Assets/Prefabs/Wheel_Model.prefab', 'r', encoding='utf-8') as f:
    content = f.read()

# Try to find SphereCollider or CapsuleCollider
colliders = re.findall(r'(SphereCollider|CapsuleCollider|WheelCollider):\n.*?m_Radius: ([-\d.]+)', content, re.DOTALL)
for c in colliders:
    print(f"Wheel_Model Collider: {c[0]} Radius: {c[1]}")
    
meshes = re.findall(r'MeshFilter:\n.*?m_Mesh: {fileID: (\d+).*?}', content, re.DOTALL)
print(f"Meshes: {meshes}")
