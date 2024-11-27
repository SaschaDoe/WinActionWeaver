using WinActionWeaver.Core.KeyMapping;
using WinActionWeaverTests.TestHelpers;

namespace WinActionWeaverTests;

public class KeyRemappingTests
{
    [Test]
    public async Task WhenKeyIsRemapped_ShouldInterceptAndSimulateNewKey()
    {
        using var logger = new TestLogger(nameof(WhenKeyIsRemapped_ShouldInterceptAndSimulateNewKey));
        
        try
        {
            logger.Log("Creating KeyMapper");
            using var keyMapper = new KeyMapper();
            
            var originalKey = VirtualKey.A;
            var newKey = VirtualKey.B;
            var interceptedKeys = new List<VirtualKey>();
            var tcs = new TaskCompletionSource<bool>();

            keyMapper.KeyPressed += (sender, key) =>
            {
                logger.Log($"Key pressed event received: {key}");
                interceptedKeys.Add(key);
                if (key == newKey)
                {
                    logger.Log("Target key detected, completing test");
                    tcs.TrySetResult(true);
                }
            };

            // Act
            logger.Log($"Remapping key {originalKey} to {newKey}");
            keyMapper.RemapKey(originalKey, newKey);
            
            // Give the hook a moment to initialize
            logger.Log("Waiting for hook initialization");
            await Task.Delay(500);
            
            logger.Log("Simulating key press...");
            var simulator = new KeyboardSimulator();
            simulator.SimulateKeyPress(originalKey);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            cts.Token.Register(() => 
            {
                logger.Log("Test timed out!");
                logger.Log($"Current intercepted keys count: {interceptedKeys.Count}");
                foreach (var key in interceptedKeys)
                {
                    logger.Log($"Previously intercepted key: {key}");
                }
                tcs.TrySetCanceled();
            });
            
            try
            {
                await tcs.Task;
                
                logger.Log($"Test completed successfully");
                logger.Log($"Intercepted keys count: {interceptedKeys.Count}");
                foreach (var key in interceptedKeys)
                {
                    logger.Log($"Intercepted key: {key}");
                }
                
                Assert.That(interceptedKeys, Has.Count.EqualTo(1), "Expected exactly one key press");
                Assert.That(interceptedKeys[0], Is.EqualTo(newKey), "Wrong key was triggered");
            }
            catch (TaskCanceledException)
            {
                Assert.Fail($"Test timed out waiting for key event. Intercepted {interceptedKeys.Count} keys.");
            }
        }
        catch (Exception ex)
        {
            logger.Log($"Test failed with exception: {ex}");
            throw;
        }
    }
} 