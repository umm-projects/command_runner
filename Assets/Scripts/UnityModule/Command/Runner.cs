using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UniRx;

namespace UnityModule.Command
{
    public static class Runner<TResult> where TResult : class
    {
        public const double DefaultTimeoutSeconds = 30.0;

        public static TResult Run(string command, string subCommand, List<string> argumentMap = null, double timeout = DefaultTimeoutSeconds)
        {
            if (typeof(TResult).IsGenericType && typeof(IObservable<>).IsAssignableFrom(typeof(TResult).GetGenericTypeDefinition()))
            {
                return RunCommandAsync(command, subCommand, argumentMap, timeout) as TResult;
            }

            return RunCommand(command, subCommand, argumentMap, timeout) as TResult;
        }

        private static IObservable<string> RunCommandAsync(string command, string subCommand, List<string> argumentMap = null, double timeout = DefaultTimeoutSeconds)
        {
            return Observable
                .Create<string>(
                    (observer) =>
                    {
                        try
                        {
                            observer.OnNext(RunCommand(command, subCommand, argumentMap, timeout));
                            observer.OnCompleted();
                        }
                        catch (Exception e)
                        {
                            observer.OnError(e);
                        }

                        return null;
                    }
                );
        }

        private static string RunCommand(string command, string subCommand, List<string> argumentMap = null, double timeoutSeconds = DefaultTimeoutSeconds)
        {
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            using (var process = new Process())
            {
                process.StartInfo = CreateProcessStartInfo(command, subCommand, argumentMap);
                process.Start();

                process.OutputDataReceived += (sender, eventArgs) =>
                {
                    if (eventArgs.Data != null)
                    {
                        stdout.AppendLine(eventArgs.Data);
                    }
                };
                process.ErrorDataReceived += (sender, eventArgs) =>
                {
                    if (eventArgs.Data != null)
                    {
                        stderr.AppendLine(eventArgs.Data);
                    }
                };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var timeouted = false;
                if (!process.WaitForExit((int) TimeSpan.FromSeconds(timeoutSeconds).TotalMilliseconds))
                {
                    timeouted = true;
                    process.Kill();
                }

                process.CancelOutputRead();
                process.CancelErrorRead();

                if (timeouted)
                {
                    throw new TimeoutException();
                }

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(stderr.ToString());
                }
            }

            return stdout.ToString();
        }

        private static ProcessStartInfo CreateProcessStartInfo(string command, string subCommand, List<string> argumentMap = null)
        {
            return new ProcessStartInfo()
            {
                FileName = command,
                Arguments = string.Format("{0}{1}", subCommand, CreateArgument(argumentMap)),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
        }

        private static string CreateArgument(List<string> argumentList)
        {
            if (argumentList == default(List<string>) || argumentList.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var argument in argumentList)
            {
                sb.AppendFormat(" {0}", argument);
            }

            return sb.ToString();
        }
    }

    public class SafeList<T> : List<T>
    {
        public SafeList(List<T> original)
        {
            if (original != default(List<T>))
            {
                AddRange(original);
            }
        }
    }

    public static class Extension
    {
        public static string Combine(this IEnumerable<string> items, bool surroundDoubleQuotation = true)
        {
            var sb = new StringBuilder();
            foreach (var item in items)
            {
                sb.AppendFormat(
                    "{1}{0}",
                    surroundDoubleQuotation ? item.Quot() : item,
                    sb.Length > 0 ? " " : string.Empty
                );
            }

            return sb.ToString();
        }

        public static string Quot(this string original)
        {
            return string.Format("{1}{0}{1}", original, "\"");
        }
    }
}
