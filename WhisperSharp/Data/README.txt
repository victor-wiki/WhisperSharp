1.Download file from: https://github.com/victor-wiki/StaticResources/tree/master/assets/WhisperSharp/dat
2.Extract the dat file from the zip file and place it into this folder.
3.If you want medium or large model dat file, you can use: exportsd.save_state_dict(model.half().to("cpu").state_dict(), file) from python project(https://github.com/dotnet/TorchSharp/blob/main/src/Python/exportsd.py)