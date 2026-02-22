using System.Collections.Concurrent;

namespace ERP_Web.Components.Custom
{
    // VoiceRecognitionService.cs
    public class VoiceRecognitionService
    {
        // 添加实例管理字典
        private static readonly ConcurrentDictionary<string, VoiceRecognition> _instances =
            new ConcurrentDictionary<string, VoiceRecognition>();

        // 注册实例
        public void RegisterInstance(string instanceId, VoiceRecognition instance)
        {
            _instances[instanceId] = instance;
        }

        // 注销实例
        public void UnregisterInstance(string instanceId)
        {
            _instances.TryRemove(instanceId, out _);
        }

        // 开始识别
        public async Task StartAsync(string instanceId)
        {
            if (_instances.TryGetValue(instanceId, out var instance))
            {
                await instance.StartAsync();
            }
            else
            {
                throw new InvalidOperationException($"语音识别实例 {instanceId} 未找到");
            }
        }

        // 停止识别
        public async Task StopAsync(string instanceId)
        {
            if (_instances.TryGetValue(instanceId, out var instance))
            {
                await instance.StopAsync();
            }
        }

        // 获取实例
        public VoiceRecognition GetInstance(string instanceId)
        {
            if (_instances.TryGetValue(instanceId, out var instance))
            {
                return instance;
            }
            return null;
        }
    }

}
