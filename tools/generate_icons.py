#!/usr/bin/env python3
"""
AutoPortal 图标生成工具
从 LOGO.png 生成所有需要的图标尺寸
"""

from PIL import Image
import os
import sys

def generate_icons(source_file="Assets/LOGO.png", output_dir="Assets"):
    """生成所有需要的图标尺寸"""
    
    print("=== AutoPortal 图标生成工具 ===")
    print()
    
    # 检查源文件是否存在
    if not os.path.exists(source_file):
        print(f"错误：找不到源文件 {source_file}")
        sys.exit(1)
    
    print(f"源文件：{source_file}")
    
    # 定义需要生成的图标尺寸
    icon_sizes = [
        {"name": "Square44x44Logo.scale-200.png", "width": 88, "height": 88},
        {"name": "Square44x44Logo.targetsize-24_altform-unplated.png", "width": 24, "height": 24},
        {"name": "Square150x150Logo.scale-200.png", "width": 300, "height": 300},
        {"name": "Wide310x150Logo.scale-200.png", "width": 620, "height": 300},
        {"name": "StoreLogo.png", "width": 50, "height": 50},
        {"name": "SplashScreen.scale-200.png", "width": 1240, "height": 600},
        {"name": "LockScreenLogo.scale-200.png", "width": 48, "height": 48},
        {"name": "app.ico", "width": 32, "height": 32}
    ]
    
    try:
        # 打开源图片
        with Image.open(source_file) as img:
            print(f"源图片尺寸：{img.width}x{img.height}")
            print()
            
            # 转换为 RGBA 模式（如果需要）
            if img.mode != 'RGBA':
                img = img.convert('RGBA')
            
            # 生成各个尺寸的图标
            for size in icon_sizes:
                output_path = os.path.join(output_dir, size["name"])
                
                print(f"生成：{size['name']} ({size['width']}x{size['height']})...", end=" ")
                
                try:
                    # 使用 LANCZOS 重采样滤波器进行高质量缩放
                    resized = img.resize((size["width"], size["height"]), Image.LANCZOS)
                    resized.save(output_path, 'PNG')
                    print("完成")
                except Exception as e:
                    print(f"失败：{e}")
        
        # 生成 ICO 文件
        print()
        print("生成：app.ico...", end=" ")
        
        try:
            ico_path = os.path.join(output_dir, "app.ico")
            
            with Image.open(source_file) as img:
                # 转换为 RGBA
                if img.mode != 'RGBA':
                    img = img.convert('RGBA')
                
                # 调整大小为 256x256
                icon_img = img.resize((256, 256), Image.LANCZOS)
                
                # 保存为 ICO
                icon_img.save(ico_path, 'ICO', sizes=[(256, 256)])
                
            print("完成")
        except Exception as e:
            print(f"失败：{e}")
        
        print()
        print("=== 图标生成完成！ ===")
        print(f"生成的文件位置：{output_dir}")
        
    except Exception as e:
        print(f"\n错误：{e}")
        sys.exit(1)

if __name__ == "__main__":
    # 切换到脚本所在目录的父目录（项目根目录）
    script_dir = os.path.dirname(os.path.abspath(__file__))
    os.chdir(os.path.dirname(script_dir))
    
    generate_icons()
