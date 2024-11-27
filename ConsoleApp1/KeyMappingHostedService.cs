using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WinActionWeaver.Core.KeyMapping;

namespace WinActionWeaver.Console;

public class KeyMappingHostedService : BackgroundService
{
    private readonly IKeyMapper _keyMapper;
    private readonly ILogger<KeyMappingHostedService> _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly MessageLoop _messageLoop;

    public KeyMappingHostedService(
        IKeyMapper keyMapper,
        MessageLoop messageLoop,
        ILogger<KeyMappingHostedService> logger,
        IHostApplicationLifetime appLifetime)
    {
        _keyMapper = keyMapper;
        _messageLoop = messageLoop;
        _logger = logger;
        _appLifetime = appLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Key mapping service starting...");
            
            // Configure your key mappings here
            _keyMapper.RemapKey(VirtualKey.A, VirtualKey.B);
            
            _logger.LogInformation("Key mapping configured: A -> B");
            _logger.LogInformation("Press Ctrl+C to exit");

            // Keep the service running until cancellation is requested
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Service shutdown requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in key mapping service");
            _appLifetime.StopApplication();
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Key mapping service stopping...");
        
        try
        {
            if (_keyMapper is IDisposable disposableMapper)
            {
                disposableMapper.Dispose();
            }
            _messageLoop.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during shutdown");
        }
        
        await base.StopAsync(cancellationToken);
    }
} 