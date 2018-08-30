using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UniRx;

namespace UnityModule.Command {

    public static class Runner<TResult> where TResult : class {

        private const double DEFAULT_TIMEOUT_SECONDS = 30.0;

        public static TResult Run(string command, string subCommand, List<string> argumentMap = null) {
            if (typeof(TResult).IsGenericType && typeof(IObservable<>).IsAssignableFrom(typeof(TResult).GetGenericTypeDefinition())) {
                return RunCommandAsync(command, subCommand, argumentMap) as TResult;
            }
            return RunCommand(command, subCommand, argumentMap) as TResult;
        }

        private static IObservable<string> RunCommandAsync(string command, string subCommand, List<string> argumentMap = null) {
            return Observable
                .Create<string>(
                    (observer) => {
                        try {
                            observer.OnNext(RunCommand(command, subCommand, argumentMap));
                            observer.OnCompleted();
                        } catch (Exception e) {
                            observer.OnError(e);
                        }
                        return null;
                    }
                );
        }

        private static string RunCommand(string command, string subCommand, List<string> argumentMap = null, double timeoutSeconds = DEFAULT_TIMEOUT_SECONDS) {
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            using (var process = new Process()) {
                process.StartInfo = CreateProcessStartInfo(command, subCommand, argumentMap);
                process.OutputDataReceived += (sender, e) => {
                    if (e.Data != null) {
                        stdout.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) => {
                    if (e.Data != null) {
                        stderr.AppendLine(e.Data);
                    }
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                bool timeouted = false;
                if (!process.WaitForExit((int)TimeSpan.FromSeconds(timeoutSeconds).TotalMilliseconds)) {
                    timeouted = true;
                    process.Kill();
                }

                // 結果を受け取れないコトがあるので、タイムアウトしていない場合は再度待つ
                //   See also: https://social.msdn.microsoft.com/Forums/netframework/ja-JP/04b43b9f-991c-4c1c-a507-414373e01e30/process-?forum=netfxgeneralja
                if (!timeouted)
                {
                    process.WaitForExit();
                }

                process.CancelOutputRead();
                process.CancelErrorRead();

                if (timeouted) {
                    throw new TimeoutException();
                }
                if (process.ExitCode != 0) {
                    throw new InvalidOperationException(stderr.ToString());
                }
            }
            return stdout.ToString();
        }

        private static ProcessStartInfo CreateProcessStartInfo(string command, string subCommand, List<string> argumentMap = null) {
            return new ProcessStartInfo() {
                FileName = command,
                Arguments = string.Format("{0}{1}", subCommand, CreateArgument(argumentMap)),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
        }

        private static string CreateArgument(List<string> argumentList) {
            if (argumentList == default(List<string>) || argumentList.Count == 0) {
                return string.Empty;
            }
            StringBuilder sb = new StringBuilder();
            foreach (string argument in argumentList) {
                sb.AppendFormat(" {0}", argument);
            }
            return sb.ToString();
        }

    }

    public class SafeList<T> : List<T> {

        public SafeList(List<T> original) {
            if (original != default(List<T>)) {
                this.AddRange(original);
            }
        }

    }

    public static class Extension {

        public static string Combine(this IEnumerable<string> items, bool surroundDoubleQuatation = true) {
            StringBuilder sb = new StringBuilder();
            foreach (string item in items) {
                sb.AppendFormat(
                    "{1}{0}",
                    surroundDoubleQuatation ? item.Quot() : item,
                    sb.Length > 0 ? " " : string.Empty
                );
            }
            return sb.ToString();
        }

        public static string Quot(this string original) {
            return string.Format("{1}{0}{1}", original, "\"");
        }

    }

}
