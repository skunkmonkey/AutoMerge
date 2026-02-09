using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Reflection;
using AutoMerge.Core.Models;
using AutoMerge.App.Localization;

namespace AutoMerge.App.Startup;

public sealed record CliParseResult(MergeInput? MergeInput, bool ShouldExit, int ExitCode, bool WaitForGui, bool NoGui, bool IsDiffOnly = false);

public static class CliParser
{
	public static CliParseResult Parse(string[] args)
	{
		if (HasFlag(args, "--help", "-h"))
		{
			Console.WriteLine(AppStrings.CliHelpText);
			return new CliParseResult(null, true, 0, true, false);
		}

		if (HasFlag(args, "--version", "-v"))
		{
			Console.WriteLine(string.Format(CultureInfo.CurrentCulture, AppStrings.CliVersionFormat, GetVersionString()));
			return new CliParseResult(null, true, 0, true, false);
		}

		if (args.Length == 0)
		{
			return new CliParseResult(null, false, 0, true, false);
		}
		string? basePath = null;
		string? localPath = null;
		string? remotePath = null;
		string? mergedPath = null;
		var waitForGui = true;
		var noGui = false;
		var positionals = new List<string>();

		for (var i = 0; i < args.Length; i++)
		{
			var arg = args[i];

			if (arg.StartsWith("--base=", StringComparison.OrdinalIgnoreCase))
			{
				basePath = arg["--base=".Length..];
				continue;
			}

			if (string.Equals(arg, "--base", StringComparison.OrdinalIgnoreCase))
			{
				if (i + 1 >= args.Length)
				{
					Console.WriteLine(AppStrings.CliHelpText);
					return new CliParseResult(null, true, 1, waitForGui, noGui);
				}

				basePath = args[++i];
				continue;
			}

			if (arg.StartsWith("--local=", StringComparison.OrdinalIgnoreCase))
			{
				localPath = arg["--local=".Length..];
				continue;
			}

			if (string.Equals(arg, "--local", StringComparison.OrdinalIgnoreCase))
			{
				if (i + 1 >= args.Length)
				{
					Console.WriteLine(AppStrings.CliHelpText);
					return new CliParseResult(null, true, 1, waitForGui, noGui);
				}

				localPath = args[++i];
				continue;
			}

			if (arg.StartsWith("--remote=", StringComparison.OrdinalIgnoreCase))
			{
				remotePath = arg["--remote=".Length..];
				continue;
			}

			if (string.Equals(arg, "--remote", StringComparison.OrdinalIgnoreCase))
			{
				if (i + 1 >= args.Length)
				{
					Console.WriteLine(AppStrings.CliHelpText);
					return new CliParseResult(null, true, 1, waitForGui, noGui);
				}

				remotePath = args[++i];
				continue;
			}

			if (arg.StartsWith("--merged=", StringComparison.OrdinalIgnoreCase))
			{
				mergedPath = arg["--merged=".Length..];
				continue;
			}

			if (string.Equals(arg, "--merged", StringComparison.OrdinalIgnoreCase))
			{
				if (i + 1 >= args.Length)
				{
					Console.WriteLine(AppStrings.CliHelpText);
					return new CliParseResult(null, true, 1, waitForGui, noGui);
				}

				mergedPath = args[++i];
				continue;
			}

			if (string.Equals(arg, "--wait", StringComparison.OrdinalIgnoreCase))
			{
				waitForGui = true;
				continue;
			}

			if (arg.StartsWith("--wait=", StringComparison.OrdinalIgnoreCase))
			{
				var value = arg["--wait=".Length..];
				if (!bool.TryParse(value, out waitForGui))
				{
					Console.WriteLine(AppStrings.CliHelpText);
					return new CliParseResult(null, true, 1, waitForGui, noGui);
				}

				continue;
			}

			if (string.Equals(arg, "--no-gui", StringComparison.OrdinalIgnoreCase))
			{
				noGui = true;
				continue;
			}

			if (arg.StartsWith("-", StringComparison.Ordinal))
			{
				Console.WriteLine(AppStrings.CliHelpText);
				return new CliParseResult(null, true, 1, waitForGui, noGui);
			}

			positionals.Add(arg);
		}

		if (positionals.Count > 0)
		{
			// 2 positional args = diff mode (LOCAL REMOTE)
			// 3 positional args = merge without base (LOCAL REMOTE MERGED)
			// 4 positional args = full merge (BASE LOCAL REMOTE MERGED)
			if (positionals.Count == 2)
			{
				if (localPath is null) localPath = positionals[0];
				if (remotePath is null) remotePath = positionals[1];
			}
			else
			{
				if (basePath is null && positionals.Count > 0)
				{
					basePath = positionals[0];
				}

				if (localPath is null && positionals.Count > 1)
				{
					localPath = positionals[1];
				}

				if (remotePath is null && positionals.Count > 2)
				{
					remotePath = positionals[2];
				}

				if (mergedPath is null && positionals.Count > 3)
				{
					mergedPath = positionals[3];
				}
			}
		}

		// Determine if this is diff-only mode (local + remote, no merged output)
		var isDiffOnly = !string.IsNullOrWhiteSpace(localPath) &&
		                 !string.IsNullOrWhiteSpace(remotePath) &&
		                 string.IsNullOrWhiteSpace(mergedPath);

		if (!isDiffOnly && (string.IsNullOrWhiteSpace(localPath) || string.IsNullOrWhiteSpace(remotePath) || string.IsNullOrWhiteSpace(mergedPath)))
		{
			Console.WriteLine(AppStrings.CliHelpText);
			return new CliParseResult(null, true, 1, waitForGui, noGui);
		}

		if (string.IsNullOrWhiteSpace(basePath))
		{
			basePath = localPath;
		}

		// In diff mode, there is no merged output file
		if (isDiffOnly)
		{
			mergedPath = localPath;
		}

		try
		{
			// Resolve relative paths to absolute paths (SourceTree may pass relative paths)
			basePath = Path.GetFullPath(basePath!);
			localPath = Path.GetFullPath(localPath!);
			remotePath = Path.GetFullPath(remotePath!);
			mergedPath = Path.GetFullPath(mergedPath!);
			var mergeInput = new MergeInput(basePath, localPath, remotePath, mergedPath);
			return new CliParseResult(mergeInput, false, 0, waitForGui, noGui, isDiffOnly);
		}
		catch (ArgumentException)
		{
			Console.WriteLine(AppStrings.CliHelpText);
			return new CliParseResult(null, true, 1, waitForGui, noGui);
		}
	}

	private static bool HasFlag(string[] args, string longForm, string shortForm)
	{
		return args.Any(arg => string.Equals(arg, longForm, StringComparison.OrdinalIgnoreCase) ||
							   string.Equals(arg, shortForm, StringComparison.OrdinalIgnoreCase));
	}

	private static string GetVersionString()
	{
		return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
	}
}
