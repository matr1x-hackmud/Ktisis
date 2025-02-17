﻿using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Interface.Components;

using Ktisis.Overlay;
using Ktisis.Structs.Actor;

namespace Ktisis.Interface.Windows.ActorEdit {
	public class EditGaze {
		public unsafe static Actor* Target => EditActor.Target;

		public static Dictionary<byte, ActorGaze>? ActorControl = null; // ObjectID : ActorGaze

		public static Gizmo Gizmo = new();
		public static GazeControl? GizmoActive =  null;

		public static bool IsLinked {
			get => Ktisis.Configuration.LinkedGaze;
			set => Ktisis.Configuration.LinkedGaze = value;
		}

		// UI Code

		public unsafe static void Draw() {
			if (ActorControl == null)
				ActorControl = new();

			if (ImGuiComponents.IconButton(IsLinked ? FontAwesomeIcon.Link : FontAwesomeIcon.Unlink))
				IsLinked = !IsLinked;
			ImGui.SameLine();
			ImGui.Text(IsLinked ? "Linked" : "Unlinked");

			ImGui.Spacing();

			var id = Target->ObjectID;
			if (!ActorControl.ContainsKey(id))
				ActorControl.Add(id, new ActorGaze());

			var gaze = ActorControl[id];

			var result = false;
			if (IsLinked) {
				result |= DrawGaze(ref gaze.Other, GazeControl.All);
			} else {
				result |= DrawGaze(ref gaze.Eyes, GazeControl.Eyes);
				ImGui.Spacing();
				result |= DrawGaze(ref gaze.Head, GazeControl.Head);
				ImGui.Spacing();
				result |= DrawGaze(ref gaze.Torso, GazeControl.Torso);
			}

			if (result)
				ActorControl[id] = gaze;

			ImGui.EndTabItem();
		}

		public static bool DrawGaze(ref Gaze gaze, GazeControl type) {
			var result = false;

			var enabled = gaze.Mode != 0;
			if (ImGui.Checkbox($"{type}", ref enabled)) {
				result = true;
				gaze.Mode = enabled ? GazeMode.Target : GazeMode.Disabled;
			}

			result |= ImGui.DragFloat3($"##{type}", ref gaze.Pos, 0.005f);

			ImGui.SameLine();
			if (ImGuiComponents.IconButton($"{FontAwesomeExtensions.ToIconChar(FontAwesomeIcon.EllipsisH)}##{type}")) {
				// TODO: Place gizmo closer to character/camera.
				GizmoActive = GizmoActive == type ? null : type;
				gaze.Mode = GizmoActive != null ? GazeMode.Target : GazeMode.Disabled;
			}

			// TODO: Rotation mode.

			if (GizmoActive == type) {
				DrawGizmo(ref gaze);
				result |= true;
			}

			return result;
		}

		// Gizmo woo gizmo!!!

		public static void DrawGizmo(ref Gaze gaze) {
			if (GizmoActive == null)
				return;

			if (KtisisGui.SkeletonEditor.HasSelected()) {
				GizmoActive = null;
				return;
			}

			var _ = new Vector3(0.0f, 0.0f, 0.0f);
			Gizmo.Draw(ref gaze.Pos, ref _, ref _);
		}

		// ControlGaze Hook

		public unsafe static void Apply(Actor* actor) {
			var isValid = ActorControl != null;
			if (Ktisis.IsInGPose && isValid) {
				var id = actor->ObjectID;
				if (ActorControl!.ContainsKey(id)) {
					var gaze = ActorControl[id];
					if (gaze.Other.Mode != 0) {
						actor->LookAt(&gaze.Other, GazeControl.All);
					} else {
						if (gaze.Torso.Mode != 0)
							actor->LookAt(&gaze.Torso, GazeControl.Torso);
						if (gaze.Head.Mode != 0)
							actor->LookAt(&gaze.Head, GazeControl.Head);
						if (gaze.Eyes.Mode != 0)
							actor->LookAt(&gaze.Eyes, GazeControl.Eyes);
					}
				}
			} else if (isValid) {
				ActorControl = null;
			}
		}
	}
}