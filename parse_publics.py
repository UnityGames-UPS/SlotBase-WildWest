import os
import re

directory = '/Users/ghanshyam/Unity/SlotBase-WildWest/Assets/Scripts/'
skip_files = ['UIImageFrameAnimator.cs', 'ImageAnimation.cs', 'GameDataModels.cs']

for filename in os.listdir(directory):
    if filename.endswith('.cs') and filename not in skip_files:
        filepath = os.path.join(directory, filename)
        with open(filepath, 'r') as f:
            lines = f.readlines()
        for i, line in enumerate(lines):
            if 'public ' in line:
                print(f"{filename}:{i+1}: {line.strip()}")
