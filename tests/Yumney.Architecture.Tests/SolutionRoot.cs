using System.Runtime.CompilerServices;

namespace SmartSolutionsLab.Yumney.Architecture.Tests;

internal static class SolutionRoot
{
	public static string Path { get; } = Locate();

	public static string Src { get; } = System.IO.Path.Combine(Path, "src");

	public static string Tests { get; } = System.IO.Path.Combine(Path, "tests");

	private static string Locate([CallerFilePath] string callerFilePath = "")
	{
		var directory = new DirectoryInfo(System.IO.Path.GetDirectoryName(callerFilePath)!);
		while (directory is not null && !File.Exists(System.IO.Path.Combine(directory.FullName, "Yumney.slnx")))
		{
			directory = directory.Parent;
		}

		return directory?.FullName
			?? throw new InvalidOperationException("Yumney.slnx not found walking up from test source location.");
	}
}
