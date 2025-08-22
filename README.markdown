# WScreenshotTool

WScreenshotTool is a powerful Unity Editor extension designed to enhance and streamline the process of capturing screenshots within Unity. It offers a variety of features such as multi-camera capture, batch processing, social media export formats, transparency support, and preset management, making it an ideal tool for developers, artists, and content creators.

[Unity Asset Store] https://assetstore.unity.com/packages/tools/utilities/w-screenshot-tool-291637

## Features

- **Multi-Camera Capture**: Capture screenshots from multiple cameras in the scene with individual settings.
- **Batch Processing**: Automate screenshot captures with customizable intervals and counts.
- **Transparency Support**: Capture screenshots with transparent backgrounds for UI elements or overlays (PNG format required).
- **Social Media Formats**: Export screenshots optimized for platforms like Twitter, Instagram, YouTube, and more.
- **Resolution Presets**: Choose from predefined resolutions or set custom dimensions for captures.
- **Preset Management**: Save and load capture settings for quick reuse.
- **Capture History**: Track and manage previous captures with options to open, export, or copy file paths.
- **Advanced Options**: Fine-tune settings like JPEG quality, resolution scaling, and UI capture inclusion.

## Installation

1. **Download**: Obtain the WScreenshotTool package from the [Unity Asset Store](https://assetstore.unity.com/publishers/46701) or the provided source.
2. **Import**: Import the package into your Unity project via `Assets > Import Package > Custom Package` or drag-and-drop the `.unitypackage` file into the Project window.
3. **Access**: Open the tool via the Unity Editor menu: `Tools > W Screenshot Tool` or `Window > Photography > W Screenshot Tool`.

## Usage

### Opening the Tool
- Access the tool through the Unity Editor menu: `Tools > W Screenshot Tool` (shortcut: `Ctrl+Shift+S` or `Cmd+Shift+S` on macOS).
- Alternatively, use `Window > Photography > W Screenshot Tool`.

### Basic Tab
- **Screenshot Name**: Set the base name for screenshot files.
- **Save Path**: Specify the output directory (default: `Assets/Screenshots`). Use the "Browse" button to select a folder.
- **Image Format**: Choose between PNG (for transparency) or JPG (with adjustable quality).
- **Resolution Scale**: Adjust the resolution multiplier (0.25x to 8x).
- **Options**:
  - **Auto Timestamp**: Append a timestamp to filenames.
  - **Console Log**: Show capture details in the Unity Console.
  - **Auto Refresh Assets**: Automatically refresh the Asset Database after capture.
  - **Open After Capture**: Open the screenshot in the default system image viewer.
- **Capture Buttons**:
  - **Capture Screenshot**: Takes a single screenshot with current settings.
  - **Quick Capture**: Takes a fast screenshot with optimized settings (1x scale, PNG, no transparency).
  - **Game View**: Captures the Game View.
  - **Scene View**: Captures the Scene View.

### Advanced Tab
- **Capture UI Elements**: Include or exclude UI elements in captures.
- **Transparent Background**: Enable for PNG captures with transparent backgrounds (requires a camera with solid color clear flags and alpha set to 0).
- **Custom Resolution**: Set specific width and height or choose from predefined resolution presets (e.g., 4K, 1080p, Instagram Square).
- **Aspect Ratio Buttons**: Quickly set common aspect ratios (16:9, 4:3, 1:1).

### Batch Tab
- Enable **Batch Mode** to capture multiple screenshots automatically.
- **Batch Count**: Set the number of screenshots to capture (1–1000).
- **Interval**: Define the time between captures (0.1–300 seconds).
- **Options**:
  - **Auto Increment Names**: Append incremental numbers to filenames.
  - **Create Batch Subfolder**: Save batch captures in a timestamped subfolder.
- **Controls**: Start, Stop, Pause, or Resume batch capture.
- **Status**: Monitor progress, elapsed time, and estimated time remaining.

### Cameras Tab
- Manage multiple cameras for capturing different perspectives.
- **Refresh Camera List**: Updates the list of scene cameras.
- **Select All Active**: Selects all active cameras in the hierarchy.
- **Camera Options**:
  - Toggle selection for each camera.
  - Set custom names for captured files.
  - View camera details (resolution, FOV, depth, clear flags).
  - Capture individual cameras or all selected cameras.
- **Tips**:
  - For transparency, set camera Clear Flags to "Solid Color" with alpha 0.
  - Use PNG format for transparent captures.

### Social Tab
- Export screenshots optimized for social media platforms.
- **Available Formats**: View and edit predefined formats (e.g., Twitter Post, Instagram Story).
- **Actions**:
  - **Export Last**: Export the most recent screenshot in a specific format.
  - **Edit**: Modify format settings (platform, name, resolution, quality).
  - **Delete**: Remove a format.
  - **Add Custom Format**: Create a new format with custom settings.
  - **Add Popular Formats**: Add common social media formats.
  - **Export Last to All Formats**: Export the most recent screenshot to all configured formats.

### Presets Tab
- Save and manage capture settings for quick reuse.
- **Save Current as Preset**: Save the current settings as a new preset.
- **Load Default Presets**: Add default presets (High Quality PNG, Transparent UI, Social Media).
- **Preset Actions**:
  - **Load**: Apply preset settings.
  - **Update**: Update preset with current settings.
  - **Rename**: Change preset name.
  - **Delete**: Remove a preset.
  - **Clear All**: Remove all presets.

### History Tab
- View and manage recent captures (up to 1000 entries).
- **Entry Details**: File name, timestamp, resolution, format, and transparency status.
- **Actions**:
  - **Open**: Open the screenshot in the default system viewer.
  - **Show**: Reveal the file in the system file explorer.
  - **Copy Path**: Copy the file path to the clipboard.
  - **Export**: Re-capture using the same settings.
- **Management**:
  - **Refresh History**: Remove entries for missing files.
  - **Export History**: Save history as a JSON file.
  - **Clear History**: Remove all history entries.

### Footer
- **Website**: Visit [Wiskered Studio](https://wiskered.com).
- **Products**: Explore other tools on the [Unity Asset Store](https://assetstore.unity.com/publishers/46701).
- **Docs**: Access the [documentation](https://drive.google.com/file/d/1TjkVXDEcrtglgshtSB4sYQiWr3srlM1J/view?usp=sharing).
- **Support**: Contact [wiskered@gmail.com](mailto:wiskered@gmail.com).
- **Settings**: View tips for advanced settings across tabs.

## API Usage
The tool provides a static API for scripting:
- `WScreenshotTool.CaptureScreenshotAPI()`: Capture a screenshot with default settings.
- `WScreenshotTool.CaptureScreenshotAPI(ScreenshotSettings)`: Capture with custom settings.
- `WScreenshotTool.StartBatchCaptureAPI(int count, float interval)`: Start a batch capture.
- `WScreenshotTool.CaptureTransparentAPI(Camera, string filePath, int width, int height)`: Capture a transparent screenshot from a specific camera.

Example:
```csharp
WScreenshotTool.CaptureScreenshotAPI(new ScreenshotSettings
{
    screenshotName = "MyScreenshot",
    savePath = "Assets/MyScreenshots",
    format = ImageFormat.PNG,
    transparentBackground = true,
    useCustomResolution = true,
    customWidth = 1920,
    customHeight = 1080
});
```

## Tips for Best Results
- **Transparency**: Use PNG format and a camera with Clear Flags set to "Solid Color" and alpha 0.
- **Social Media**: Center important content for mobile viewing and use platform-specific resolutions.
- **Batch Processing**: Use short intervals for time-lapse captures or longer intervals for scene changes.
- **Performance**: Disable unnecessary cameras and lower resolution multipliers for faster captures.
- **Presets**: Save frequently used settings to streamline workflows.

## Support
For issues, suggestions, or feature requests, contact [wiskered@gmail.com](mailto:wiskered@gmail.com) with the subject "W Screenshot Tool v2.0.0 Support".

## License
WScreenshotTool is licensed for **free use** with attribution. You may use, modify, and distribute the tool in your projects, provided you include the following attribution in your documentation or credits:

> WScreenshotTool by Wiskered Studio (https://wiskered.com)

Commercial and non-commercial use is permitted as long as the attribution is included. For full license details, see the LICENSE.txt.