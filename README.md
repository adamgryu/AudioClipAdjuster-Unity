# Audio Clip Adjuster for Unity
An extension for Unity that calls into [FFmpeg](https://ffmpeg.org/) to adjust audio clips quickly.

### Features
- Change the volume of an audio clip with the click of a button. No need to open Audacity!
- Change the pitch or tempo at the same time, if you'd like.
- Select multiple clips in Unity to edit them as a batch.
- Keeps the original clip in the `Temp` folder so it can be restored (as long as you don't close Unity). 

### Requirements
You must have FFmpeg installed. It is a command line tool for converting and editing audio and video.

## Installation
1. Ensure you have FFmpeg installed.
    - Run `ffmpeg` in the [command line](https://www.wikihow.com/Open-the-Command-Prompt-in-Windows) to see if it's already installed.
    - If not, download and unpack [FFmpeg](https://ffmpeg.org/download.html#build-windows) to your system.
    - Ideally, add FFmpeg to your [PATH](https://medium.com/@kevinmarkvi/how-to-add-executables-to-your-path-in-windows-5ffa4ce61a53). If you skip this step, you'll have to manually point to FFmpeg in the Audio Clip Adjuster settings.
2. Install Audio Clip Adjuster via the Unity Package Manager.
    - Open the Package Manager, press the `+` button, and select "Add package from git URL."
    - Copy in this URL: `https://github.com/adamgryu/AudioClipAdjuster-Unity.git`

## How To Use
- To open the Audio Clip Adjuster window, select an audio clip.
- In the inspector open the audio clip context menu (by clicking the three dots icon) and select "Edit Volume."
- Adjust the parameters and press "Adjust Clip" to modify the audio clip asset.
- Press "Restore Cached" to revert the clip to its original form. This option is only available until you close Unity.
- Press "Overwrite Cached" to destroy the original clip and apply these changes permanently. If you want to increase the volume above 200% you'll need to do this.
