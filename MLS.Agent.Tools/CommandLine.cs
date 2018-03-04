﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Clockwise;
using Pocket;
using Recipes;
using static Pocket.Logger<MLS.Agent.Tools.CommandLine>;

namespace MLS.Agent.Tools
{
    public static class CommandLine
    {
        public static Task<CommandLineResult> Execute(
            FileInfo exePath,
            string args,
            DirectoryInfo workingDir = null,
            Budget budget = null) =>
            Execute(exePath.FullName,
                    args,
                    workingDir,
                    budget);

        public static async Task<CommandLineResult> Execute(
            string command,
            string args,
            DirectoryInfo workingDir = null,
            Budget budget = null)
        {
            args = args ?? "";
            budget = budget ?? new Budget();

            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();

            using (var operation = CheckBudgetAndStartConfirmationLogger(command, args, budget))
            using (var process = StartProcess(
                command,
                args,
                workingDir,
                output: data =>
                {
                    stdOut.AppendLine(data);
                    operation.Info("{data}", data);
                },
                error: data =>
                {
                    stdErr.AppendLine(data);
                    operation.Error("{data}", args: data);
                }))
            {
                var exitCode = await process.Complete(budget);

                var result = new CommandLineResult(
                    exitCode: exitCode,
                    output: stdOut.Replace("\r\n", "\n").ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries),
                    error: stdErr.Replace("\r\n", "\n").ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries));

                if (exitCode == 0)
                {
                    operation.Succeed(
                        "{command} {args} exited with {code}",
                        process.StartInfo.FileName,
                        process.StartInfo.Arguments,
                        process.ExitCode);
                }
                else
                {
                    var ex = new BudgetExceededException(budget);
                    operation.Fail(ex);
                }

                return result;
            }
        }

        public static async Task<int> Complete(
            this Process process,
            Budget budget = null) =>
            await Task.Run(() =>
                      {
                          process.WaitForExit();

                          return process.ExitCode;
                      })
                      .CancelIfExceeds(
                          budget,
                          ifCancelled: () =>
                          {
                              Task.Run(() =>
                              {
                                  if (!process.HasExited)
                                  {
                                      process.Kill();
                                  }
                              }).DontAwait();

                              return 124; // like the Linux timeout command 
                          });

        public static Process StartProcess(
            string command,
            string args,
            DirectoryInfo workingDir,
            Action<string> output = null,
            Action<string> error = null,
            params (string key, string value)[] environmentVariables)
        {
            using (var operation = Log.OnEnterAndExit())
            {
                args = args ?? "";

                var process = new Process
                {
                    StartInfo =
                    {
                        Arguments = args,
                        FileName = command,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        RedirectStandardInput = true,
                        WorkingDirectory = workingDir?.FullName
                    }
                };

                if (environmentVariables?.Length > 0)
                {
                    foreach (var tuple in environmentVariables)
                    {
                        operation.Trace("Adding environment variable {tuple}", tuple);
                        process.StartInfo.EnvironmentVariables.Add(tuple.key, tuple.value);
                    }
                }

                if (output != null)
                {
                    process.OutputDataReceived += (sender, eventArgs) =>
                    {
                        if (eventArgs.Data != null)
                        {
                            output(eventArgs.Data);
                        }
                    };
                }

                if (error != null)
                {
                    process.ErrorDataReceived += (sender, eventArgs) =>
                    {
                        if (eventArgs.Data != null)
                        {
                            error(eventArgs.Data);
                        }
                    };
                }

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                return process;
            }
        }

        internal static string AppendArgs(this string initial, string append = null) =>
            string.IsNullOrWhiteSpace(append)
                ? initial
                : $"{initial} {append}";

        private static ConfirmationLogger CheckBudgetAndStartConfirmationLogger(
            object command,
            string args,
            Budget budget,
            [CallerMemberName] string operationName = null)
        {
            budget.RecordEntryAndThrowIfBudgetExceeded($"Execute ({command} {args})");

            return new ConfirmationLogger(
                operationName: operationName,
                category: Log.Category,
                message: "Invoking {command} {args}",
                args: new[] { command, args },
                logOnStart: true);
        }
    }
}
