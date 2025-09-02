using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

public sealed class VoiceActionTool : MonoBehaviour
{
    [Header("AI calls these events")]
    public UnityEvent onStartCubeAnimation;
    public UnityEvent onStopCubeAnimation;
    public UnityEvent<float> onSetRotationSpeed;

    // Tool method signatures to match ToolBindings expectations
    public async Task<JObject> Actions_Start(JObject args)
    {
        onStartCubeAnimation?.Invoke();
        await Task.Yield();
        return new JObject { ["ok"] = true };
    }

    public async Task<JObject> Actions_Stop(JObject args)
    {
        onStopCubeAnimation?.Invoke();
        await Task.Yield();
        return new JObject { ["ok"] = true };
    }

    public async Task<JObject> Actions_SetSpeed(JObject args)
    {
        var speedDouble = args?.Value<double?>("speed") ?? 90.0;
        float speed = (float)speedDouble;
        onSetRotationSpeed?.Invoke(speed);
        await Task.Yield();
        return new JObject { ["ok"] = true, ["speed"] = speed };
    }
}
