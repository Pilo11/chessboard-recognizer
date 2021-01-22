using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ChessService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChessController : ControllerBase
    {
        [HttpGet]
        public string Get()
        {
            return "Chess API";
        }

        private string _move = string.Empty;

        [HttpPost("result")]
        public async Task<string> Result([FromHeader] string player)
        {
            var workingDirectory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".."));
            var targetFilename = Path.Combine(workingDirectory,"chess.png");
            System.IO.File.Delete(targetFilename);

            await using (var fw = new FileStream(targetFilename, FileMode.OpenOrCreate, FileAccess.Write))
            {
                await Request.Body.CopyToAsync(fw);
            }

            Console.WriteLine($"{DateTime.UtcNow}: current work directory: {workingDirectory}");
            Console.WriteLine($"{DateTime.UtcNow}: chess image imported: {new FileInfo(targetFilename).Length} bytes");
            
            var process = new Process
            {
                StartInfo =
                {
                    FileName = "python",
                    Arguments = "recognize.py -q chess.png",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = workingDirectory
                }
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var err = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            
            var retString = string.Empty;
            retString += $"Player ({player}): ";
            
            Regex fenRegex = new Regex(@"^[rnbqkpRNBQKP0-9]+\/[rnbqkpRNBQKP0-9]+\/[rnbqkpRNBQKP0-9]+\/[rnbqkpRNBQKP0-9]+\/[rnbqkpRNBQKP0-9]+\/[rnbqkpRNBQKP0-9]+\/[rnbqkpRNBQKP0-9]+\/[rnbqkpRNBQKP0-9]+", RegexOptions.Multiline);
            if (fenRegex.IsMatch(output))
            {
                var fen = fenRegex.Match(output).Value;
                if (player == "b")
                {
                    fen = InvertFen(fen);
                }
                ProcessStartInfo si = new ProcessStartInfo() {
                    FileName = @"/usr/local/bin/stockfish",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                };

                var myProcess = new Process();
                myProcess.StartInfo = si;
                myProcess.OutputDataReceived += new DataReceivedEventHandler(myProcess_OutputDataReceived);
                myProcess.Start();
                myProcess.BeginErrorReadLine();
                myProcess.BeginOutputReadLine();
                
                SendLine(myProcess, "ucinewgame");
                SendLine(myProcess, $"position fen {fen} {player}");
                SendLine(myProcess, "go");
                await Task.Delay(2000);
                SendLine(myProcess, "quit");
                myProcess.WaitForExit();
                if (!string.IsNullOrWhiteSpace(_move))
                {
                    retString += $"MOVE: {_move}";
                }
                _move = string.Empty;
            }
            
            if (!string.IsNullOrWhiteSpace(err))
            {
                retString += $"{Environment.NewLine}ERROR: {err}";
            }
            return retString;
        }
        
        private string InvertFen(string fen)
        {
            var fenGroups = fen.Split("/");
            var invertedFenGroups = new List<string>();
            foreach (var fenGroup in fenGroups.Reverse())
            {
                invertedFenGroups.Add(new string(fenGroup.Reverse().ToArray()));
            }
            return string.Join('/', invertedFenGroups);
        }
        
        private void SendLine(Process process, string command) {
            process.StandardInput.WriteLine(command);
            process.StandardInput.Flush();
        }

        private void myProcess_OutputDataReceived(object sender, DataReceivedEventArgs e) {
            string text = e.Data;
            if (text?.StartsWith("bestmove") ?? false)
            {
                _move = text.Split(' ')[1];
            }
        }
    }
}