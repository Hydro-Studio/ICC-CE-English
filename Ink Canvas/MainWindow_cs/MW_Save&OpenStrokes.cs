using Ink_Canvas.Helpers;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using File = System.IO.File;

namespace Ink_Canvas {
    public partial class MainWindow : Window {
        private void SymbolIconSaveStrokes_MouseUp(object sender, MouseButtonEventArgs e) {
            if (lastBorderMouseDownObject != sender || inkCanvas.Visibility != Visibility.Visible) return;

            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);

            GridNotifications.Visibility = Visibility.Collapsed;

            SaveInkCanvasStrokes(true, true);
        }

        private void SaveInkCanvasStrokes(Boolean newNotice, Boolean saveByUser) {
            try {
                // 修改保存路径为软件根目录下的Saves文件夹
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                if (string.IsNullOrEmpty(appDirectory))
                {
                    appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                }
                
                string savePath = Path.Combine(appDirectory, "Saves", 
                    (saveByUser ? @"User Saved - " : @"Auto Saved - ") + 
                    (currentMode == 0 ? "Annotation Strokes" : "BlackBoard Strokes"));
                
                if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
                string savePathWithName;
                if (currentMode != 0) // 黑板模式下
                    savePathWithName = savePath + @"\" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff") + " Page-" + 
                               CurrentWhiteboardIndex + " StrokesCount-" + inkCanvas.Strokes.Count + ".icstk";
                else
                    savePathWithName = savePath + @"\" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff") + ".icstk";

                try {
                    using (FileStream fs = new FileStream(savePathWithName, FileMode.Create)) { 
                        inkCanvas.Strokes.Save(fs);
                    }
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is DirectoryNotFoundException) {
                    // 修改异常处理中的备用路径为软件根目录下的Saves文件夹
                    string fallbackPath = Path.Combine(appDirectory, "Saves");
                    Directory.CreateDirectory(fallbackPath);
                    
                    string fileName = Path.GetFileNameWithoutExtension(savePathWithName) + "_retry.icstk";
                    string newPath = Path.Combine(fallbackPath, fileName);

                    try {
                        using (FileStream fs = new FileStream(newPath, FileMode.Create)) {
                            inkCanvas.Strokes.Save(fs);
                            savePathWithName = newPath;
                        }
                    }
                    catch (Exception fallbackEx) {
                        ShowNotification($"Stroke save failed: {fallbackEx.Message}");
                        return;
                    }
                }
                catch (Exception ex) {
                    ShowNotification($"Stroke save failed: {ex.Message}");
                    return;
                }

                if (newNotice) ShowNotification("Stroke successfully saved to " + savePathWithName);
            }
            catch (Exception ex) {
                ShowNotification("Stroke save failed");
                LogHelper.WriteLogToFile("墨迹保存失败 | " + ex.ToString(), LogHelper.LogType.Error);
            }
        }

        private void SymbolIconOpenStrokes_MouseUp(object sender, MouseButtonEventArgs e) {
            if (lastBorderMouseDownObject != sender) return;
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);

            var openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "打开墨迹文件";
            openFileDialog.Filter = "Ink Canvas Strokes File (*.icstk)|*.icstk";
            if (openFileDialog.ShowDialog() != true) return;
            LogHelper.WriteLogToFile($"Strokes Insert: Name: {openFileDialog.FileName}",
                LogHelper.LogType.Event);
            try {
                var fileStreamHasNoStroke = false;
                using (var fs = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read)) {
                    var strokes = new StrokeCollection(fs);
                    fileStreamHasNoStroke = strokes.Count == 0;
                    if (!fileStreamHasNoStroke) {
                        ClearStrokes(true);
                        timeMachine.ClearStrokeHistory();
                        inkCanvas.Strokes.Add(strokes);
                        LogHelper.NewLog($"Strokes Insert: Strokes Count: {inkCanvas.Strokes.Count.ToString()}");
                    }
                }

                if (fileStreamHasNoStroke)
                    using (var ms = new MemoryStream(File.ReadAllBytes(openFileDialog.FileName))) {
                        ms.Seek(0, SeekOrigin.Begin);
                        var strokes = new StrokeCollection(ms);
                        ClearStrokes(true);
                        timeMachine.ClearStrokeHistory();
                        inkCanvas.Strokes.Add(strokes);
                        LogHelper.NewLog($"Strokes Insert (2): Strokes Count: {strokes.Count.ToString()}");
                    }

                if (inkCanvas.Visibility != Visibility.Visible) SymbolIconCursor_Click(sender, null);
            }
            catch {
                ShowNotification("Stroke open failed");
            }
        }
    }
}