using tLogViewer.Services;

var logProcessor = new LogProcessor();
var messages = logProcessor.ProcessLogFile("D:\\projects\\tLogViewer\\data\\2026-06-04 18-42-04.tlog");

foreach (var message in messages)
{
    message.Print();
}
