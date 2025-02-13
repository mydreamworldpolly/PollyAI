# PollyAI (The lightest and most portable AI client in the world - only 1MB in size.)

<div align="center">

[English](#english-version) | [中文](#chinese-version)

</div>

<a id="english-version"></a>
## English Version

### Overview
PollyAI Notepad is a minimalist AI Windows client designed for academic and professional environments. With its compact size and discreet interface, it offers powerful AI capabilities while maintaining a low profile.
<img width="947" alt="image" src="https://github.com/user-attachments/assets/97d33868-c0c3-419a-9637-250e4f63f40c" />
<img width="950" alt="image" src="https://github.com/user-attachments/assets/6760bf13-cd05-4a3b-9864-0805fab004ca" />

### Key Features
- Portable: No installation required, just copy and paste to use
- Text conversations with AI models
- Image recognition and analysis
- Bing online search integration (beta)
- Image generation using Flux and DALLE-3
- Customizable prompts and knowledge base using simple text files
- Clean, non-AI-like interface for discrete usage

### Technical Specifications
- Built with C# and .NET Framework 4.8
- Compatible with Windows 10 and Windows 11
- Supports multiple AI providers:
  - Azure OpenAI
  - OpenRouter (or other openai-compatible API)
  - Ollama
  - DeepSeek

### System Requirements
- Windows 10 or Windows 11
- .NET Framework 4.8
- Valid API keys for chosen AI providers

### Installation
1. Download the five files from the release section
2. Configure `api.json` with your AI provider endpoints and API keys
3. (Optional) Set up `search.json` with Azure Bing API key for search functionality
4. (Optional) Configure `image.json` with Replicate API key for Flux model and/or Azure API key for DALLE-3

### Usage Instructions
1. Launch the executable file
2. Interface Overview:
   - Conversations are displayed in a table format
   - Click row headers to select rows (Delete key removes selected rows)
   - First column: Role (blank = user, assistant = AI response, system = system prompt)
   - Second column: Conversation content (double-click to edit)
   - Third column: Images (click to upload/view)

3. Basic Operations:
   - Use 'Send' button for text conversations
   - Select AI models from the dropdown (configured in api.json)
   - Adjust maxtoken to control response length
   - Use creative slider to balance between conservative and innovative responses

4. Advanced Features (Copilot):
   - Click 'Copilot' button for search and image generation
   - Image generation requires either Flux (via Replicate) or DALLE-3 (via Azure) configuration
   - Generated images are saved to desktop by default
  
5. Custom Prompts and Memories:
   - Create .txt files in the same folder as the executable
   - Files will automatically appear in the Prompts and Memory dropdowns
   - Use these files to store frequently used prompts or custom knowledge
   - Example: Define custom translations or behaviors (e.g., "Polly" must be translated as "glass" in Chinese)
   - Selected prompt and memory content are automatically injected into conversations when using Send or Copilot buttons

### Version
0.1.0 (Beta)

### Author
Polly

---

<a id="chinese-version"></a>
## Chinese Version (中文版本)

### 项目概述
PollyAI便签是一款为学术和职业环境设计的Windows AI迷你客户端。它体积小巧，界面简洁，提供强大的AI功能的同时保持低调的外观。

### 主要特点
- 便携：无需安装，复制即用
- AI文字对话功能
- 图片识别和分析
- Bing在线搜索集成（测试版）
- 支持Flux和DALLE-3的图像生成
- 通过文本文件自定义prompt和知识库
- 清爽的非AI式界面设计，便于隐身使用

### 技术规格
- 使用C#和.NET Framework 4.8开发
- 支持Windows 10和Windows 11
- 支持多个AI服务商：
  - Azure OpenAI
  - OpenRouter (理论上也兼容其他的 openai兼容api)
  - Ollama
  - DeepSeek

### 系统要求
- Windows 10或Windows 11
- .NET Framework 4.8
- 相应AI服务商的有效API密钥

### 安装配置
1. 从release section下载五个文件
2. 在api.json中配置AI服务商的endpoint和API密钥
3. （可选）在search.json中配置Azure Bing API密钥以启用搜索功能
4. （可选）在image.json中配置Replicate的Flux模型密钥或Azure的DALLE-3密钥

### 使用说明
1. 运行exe可执行文件
2. 界面说明：
   - 对话以表格形式显示
   - 点击行头选中该行（按Delete键可删除）
   - 第一列：角色（空白=用户，assistant=AI回复，system=系统prompt）
   - 第二列：对话内容（双击可编辑）
   - 第三列：图片（点击可上传/查看）

3. 基本操作：
   - 点击"Send"按钮进行文字对话
   - 从下拉菜单选择AI模型（来自api.json配置）
   - 通过maxtoken控制生成文字长度
   - 使用creative滑块调节AI从严谨到创新的程度

4. 高级功能（Copilot）：
   - 点击"Copilot"按钮使用搜索和生成图像功能
   - 图像生成需要配置Flux（通过Replicate）或DALLE-3（通过Azure）
   - 生成的图片默认保存在桌面
  
5. 自定义Prompt和记忆库：
   - 在程序exe所在文件夹创建txt文本文件
   - 这些文件会自动出现在Prompts和Memory下拉菜单中
   - 用于储存常用的prompt或自定义知识
   - 例如：定义特定翻译或行为（如"Polly"必须翻译成"玻璃"而不是"波莉"）
   - 使用Send或Copilot按钮时，选中的prompt和memory内容会自动注入到对话中供AI参考

### 版本
0.1.0（测试版）

### 作者
Polly投资
