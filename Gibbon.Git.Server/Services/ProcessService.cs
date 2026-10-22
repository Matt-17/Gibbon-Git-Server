using System.Diagnostics;
using System.Threading.Tasks;

namespace Gibbon.Git.Server.Services;

public class ProcessService : IProcessService
{
    public async Task<ProcessResult> StartProcessWithStreamAsync(ProcessStartInfo startInfo, Stream inStream, Stream outStream, bool endStreamWithClose, int bufferSize = 81920)
    {
        var result = new ProcessResult();

        try
        {
            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start process.");

            try
            {
                if (inStream != null)
                {
                    await inStream.CopyToAsync(process.StandardInput.BaseStream, bufferSize);
                }

                if (endStreamWithClose)
                {
                    process.StandardInput.Close();
                }
                else
                {
                    await process.StandardInput.WriteAsync('\0');
                }
            }
            catch (IOException)
            {
                // the process may exit without reading its input (git --advertise-refs), its output is still needed
            }

            await process.StandardOutput.BaseStream.CopyToAsync(outStream, bufferSize);

            await process.WaitForExitAsync();

            result.IsSuccess = process.ExitCode == 0;
            result.Output = "Process completed successfully.";
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.Error = $"An error occurred: {ex.Message}";
        }

        return result;
    }

    public async Task<ProcessResult> StartProcessAsync(ProcessStartInfo startInfo)
    {
        var result = new ProcessResult();
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                result.IsSuccess = false;
                result.Error = "Failed to start process.";
                return result;
            }

            outputBuilder.Append(await process.StandardOutput.ReadToEndAsync());
            errorBuilder.Append(await process.StandardError.ReadToEndAsync());

            await process.WaitForExitAsync();

            result.Output = outputBuilder.ToString();
            result.Error = errorBuilder.ToString();
            result.IsSuccess = process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.Error = $"An error occurred: {ex.Message}";
        }

        return result;
    }
}
