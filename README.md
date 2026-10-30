This project is C# version of [openai/whisper](https://github.com/openai/whisper). Currently, the project only supports cpu mode.

1. **PyTorch** uses pt file, but **TorchSharp** uses dat file, please refer to the [README](https://github.com/victor-wiki/WhisperSharp/tree/master/WhisperSharp/Data#readme).
2. The project supports **FFMpeg** and **NAudio**, if uses **FFMpeg**, please download the **ffmpeg.exe** from [offical website](https://ffmpeg.org/).

**Note:** The main performance issue is the memory would be taken too much, because the [NumSharp](https://github.com/SciSharp/NumSharp) is not perfect as numpy currently.
