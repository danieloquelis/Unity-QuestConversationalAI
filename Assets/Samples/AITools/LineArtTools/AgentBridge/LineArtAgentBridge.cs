using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LineArtTools
{
	/// <summary>
	/// Agent-accessible bridge exposing LineArtTools, CharacterFactory, AnimationTool, MRPlacementTool, and Sequencer.
	/// Methods conform to ToolBindings expectations: Task<JObject> Fn(JObject args).
	/// </summary>
	public sealed class LineArtAgentBridge : MonoBehaviour
	{
		[SerializeField] private LineArtTool lineArt;
		[SerializeField] private CharacterFactory characters;
		[SerializeField] private AnimationTool animationTool;
		// Sequencer removed per user request

		private void Awake()
		{
			if (lineArt == null) lineArt = GetComponent<LineArtTool>() ?? gameObject.AddComponent<LineArtTool>();
			if (characters == null) characters = GetComponent<CharacterFactory>() ?? gameObject.AddComponent<CharacterFactory>();
			if (animationTool == null) animationTool = GetComponent<AnimationTool>() ?? gameObject.AddComponent<AnimationTool>();
			// no sequencer setup
		}

		// CharacterFactory
		public async Task<JObject> CharacterFactory_CreateStickFigure(JObject args)
		{
			var id = args.Value<string>("id");
			var colorHex = args.Value<string>("color");
			var scale = (float?)args.Value<double?>("scale") ?? 1f;
			var color = ColorUtil.TryParseHtml(colorHex, out var c) ? c : LineArtToolsSettings.Instance.defaultLineColor;
			characters.CreateStickFigure(id, color, scale);
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		// LineArt primitives
		public async Task<JObject> LineArt_LineSegment(JObject args)
		{
			var id = args.Value<string>("id");
			var a = ToVec3(args["a"]); var b = ToVec3(args["b"]);
			var width = (float?)args.Value<double?>("width") ?? LineArtToolsSettings.Instance.defaultLineWidth;
			var color = ParseColor(args.Value<string>("color"));
			lineArt.LineSegment(id, a, b, width, color);
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		public async Task<JObject> LineArt_Circle(JObject args)
		{
			var id = args.Value<string>("id");
			var center = ToVec3(args["center"]);
			var radius = (float?)args.Value<double?>("radius") ?? 0.1f;
			var width = (float?)args.Value<double?>("width") ?? LineArtToolsSettings.Instance.defaultLineWidth;
			var color = ParseColor(args.Value<string>("color"));
			var segments = args.Value<int?>("segments") ?? 32;
			lineArt.Circle(id, center, radius, width, color, segments);
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		public async Task<JObject> LineArt_PolylineBegin(JObject args)
		{
			var id = args.Value<string>("id");
			var width = (float?)args.Value<double?>("width") ?? LineArtToolsSettings.Instance.defaultLineWidth;
			var color = ParseColor(args.Value<string>("color"));
			lineArt.PolylineBegin(id, width, color);
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		public async Task<JObject> LineArt_PolylineAddVertex(JObject args)
		{
			var id = args.Value<string>("id");
			var p = ToVec3(args["p"]);
			lineArt.PolylineAddVertex(id, p);
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		public async Task<JObject> LineArt_PolylineEnd(JObject args)
		{
			var id = args.Value<string>("id");
			var closed = args.Value<bool?>("closed") ?? false;
			lineArt.PolylineEnd(id, closed);
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		// LineArt advanced
		public async Task<JObject> LineArt_Rotate(JObject args)
		{
			var id = args.Value<string>("id");
			var deg = (float?)args.Value<double?>("degrees") ?? 0f;
			Vector3 euler = Vector3.zero;
			var axis = args.Value<string>("axis");
			var eulerToken = args["euler"] as JArray;
			if (eulerToken != null && eulerToken.Count >= 3)
			{
				euler = new Vector3((float)eulerToken[0], (float)eulerToken[1], (float)eulerToken[2]);
			}
			else if (!string.IsNullOrEmpty(axis))
			{
				switch (axis.ToLowerInvariant())
				{
					case "x": euler = new Vector3(deg, 0f, 0f); break;
					case "y": euler = new Vector3(0f, deg, 0f); break;
					case "z": euler = new Vector3(0f, 0f, deg); break;
					default: euler = Vector3.zero; break;
				}
			}
			lineArt.Xform(id, null, euler, null);
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		public async Task<JObject> LineArt_BezierQuadratic(JObject args)
		{
			var id = args.Value<string>("id");
			var p0 = ToVec3(args["p0"]);
			var p1 = ToVec3(args["p1"]);
			var p2 = ToVec3(args["p2"]);
			var width = (float?)args.Value<double?>("width") ?? LineArtToolsSettings.Instance.defaultLineWidth;
			var color = ParseColor(args.Value<string>("color"));
			var segments = args.Value<int?>("segments") ?? 32;
			lineArt.BezierQuadratic(id, p0, p1, p2, width, color, segments);
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		public async Task<JObject> LineArt_BezierCubic(JObject args)
		{
			var id = args.Value<string>("id");
			var p0 = ToVec3(args["p0"]);
			var p1 = ToVec3(args["p1"]);
			var p2 = ToVec3(args["p2"]);
			var p3 = ToVec3(args["p3"]);
			var width = (float?)args.Value<double?>("width") ?? LineArtToolsSettings.Instance.defaultLineWidth;
			var color = ParseColor(args.Value<string>("color"));
			var segments = args.Value<int?>("segments") ?? 32;
			lineArt.BezierCubic(id, p0, p1, p2, p3, width, color, segments);
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		// Bindings
		public async Task<JObject> LineArt_BindEndpoints(JObject args)
		{
			var id = args.Value<string>("id");
			var aId = args.Value<string>("aId");
			var bId = args.Value<string>("bId");
			lineArt.BindEndpoints(id, GlobalRegistry.GetTransform(aId), GlobalRegistry.GetTransform(bId));
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		public async Task<JObject> LineArt_FollowPoint(JObject args)
		{
			var id = args.Value<string>("id");
			var targetId = args.Value<string>("targetId");
			lineArt.FollowPoint(id, GlobalRegistry.GetTransform(targetId), Vector3.zero);
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		public async Task<JObject> LineArt_SetStyle(JObject args)
		{
			var id = args.Value<string>("id");
			float? width = (float?)args.Value<double?>("width");
			Color? color = null; var colorStr = args.Value<string>("color");
			if (!string.IsNullOrEmpty(colorStr) && ColorUtil.TryParseHtml(colorStr, out var c)) color = c;
			bool? depthTest = args.Value<bool?>("depthTest");
			var dashed = args.Value<bool?>("dashed") ?? false;
			var dashSize = (float?)args.Value<double?>("dashSize") ?? 0.05f;
			var gapSize = (float?)args.Value<double?>("gapSize") ?? 0.05f;
			lineArt.SetStyle(id, width, color, depthTest, dashed, dashSize, gapSize);
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		// MR placement
		public async Task<JObject> MRPlacement_PlaceOnNearestSurface(JObject args)
		{
			var sceneRootId = args.Value<string>("sceneRootId");
			var t = GlobalRegistry.GetTransform(sceneRootId);
			if (t == null) return new JObject { ["ok"] = false, ["error"] = "Unknown sceneRootId" };
			var surface = (args.Value<string>("surface") ?? "Table").ToLowerInvariant();
			var kind = surface == "floor" ? MRSurfaceKind.Floor : (surface == "any" ? MRSurfaceKind.Any : MRSurfaceKind.Table);
			var maxDist = (float?)args.Value<double?>("maxDistanceMeters") ?? 3f;
			Transform originOverride = null;
			var originId = args.Value<string>("originId");
			if (!string.IsNullOrEmpty(originId)) originOverride = GlobalRegistry.GetTransform(originId);
			Vector3? originWorld = null;
			var originArr = args["originWorldPos"] as JArray;
			if (originArr != null && originArr.Count >= 3) originWorld = new Vector3((float)originArr[0], (float)originArr[1], (float)originArr[2]);
			MRPlacementTool.PlaceOnNearestSurface(t, kind, maxDist, originOverride, originWorld);
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		// Animation
		public async Task<JObject> Animation_MoveTo(JObject args)
		{
			var id = args.Value<string>("id");
			var pos = ToVec3(args["targetPos"]);
			var duration = (float?)args.Value<double?>("duration") ?? 1f;
			var channel = args.Value<string>("channel") ?? "default";
			var ease = args.Value<string>("ease") ?? "linear";
			animationTool.MoveTo(id, pos, duration, channel, ease);
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		public async Task<JObject> Animation_TranslateBy(JObject args)
		{
			var id = args.Value<string>("id");
			var delta = ToVec3(args["delta"]);
			var duration = (float?)args.Value<double?>("duration") ?? 1f;
			var channel = args.Value<string>("channel") ?? "default";
			var ease = args.Value<string>("ease") ?? "linear";
			animationTool.TranslateBy(id, delta, duration, channel, ease);
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		public async Task<JObject> Animation_OscillatePosition(JObject args)
		{
			var id = args.Value<string>("id");
			var axis = ToVec3(args["axis"]);
			var amp = (float?)args.Value<double?>("amplitude") ?? 0.1f;
			var freq = (float?)args.Value<double?>("frequency") ?? 1f;
			var phase = (float?)args.Value<double?>("phase") ?? 0f;
			var channel = args.Value<string>("channel") ?? "oscPos";
			animationTool.OscillatePosition(id, axis, amp, freq, phase, channel);
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		public async Task<JObject> Animation_OscillateRotation(JObject args)
		{
			var id = args.Value<string>("id");
			var axis = ToVec3(args["axis"]);
			var ampDeg = (float?)args.Value<double?>("amplitudeDeg") ?? 15f;
			var freq = (float?)args.Value<double?>("frequency") ?? 1f;
			var phase = (float?)args.Value<double?>("phase") ?? 0f;
			var channel = args.Value<string>("channel") ?? "oscRot";
			animationTool.OscillateRotation(id, axis, ampDeg, freq, phase, channel);
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		public async Task<JObject> Animation_Spin(JObject args)
		{
			var id = args.Value<string>("id");
			var axis = ToVec3(args["axis"]);
			var dps = (float?)args.Value<double?>("degPerSec") ?? 180f;
			var channel = args.Value<string>("channel") ?? "spin";
			animationTool.Spin(id, axis, dps, channel);
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		public async Task<JObject> Animation_Stop(JObject args)
		{
			var id = args.Value<string>("id");
			var channel = args.Value<string>("channel");
			animationTool.Stop(id, channel);
			await Task.Yield();
			return new JObject { ["ok"] = true };
		}

		// Sequencer entry removed

		private static Vector3 ToVec3(JToken token)
		{
			if (token is JArray arr && arr.Count >= 3)
				return new Vector3((float)arr[0], (float)arr[1], (float)arr[2]);
			return Vector3.zero;
		}

		private static Color ParseColor(string hex)
		{
			if (!string.IsNullOrEmpty(hex) && ColorUtil.TryParseHtml(hex, out var c)) return c;
			return LineArtToolsSettings.Instance.defaultLineColor;
		}
	}
}


