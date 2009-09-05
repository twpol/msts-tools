using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Diagnostics;

namespace JGR.GUI
{
	public class ToolStripNativeRenderer : ToolStripRenderer
	{
		protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e) {
			var rect = e.ArrowRectangle;
			var element = VisualStyleElement.CreateElement("menu", 16, e.Item.Enabled ? 1 : 2);
			if (VisualStyleRenderer.IsSupported && VisualStyleRenderer.IsElementDefined(element)) {
				var renderer = new VisualStyleRenderer(element);
				renderer.DrawBackground(e.Graphics, rect);
			} else {
				base.OnRenderArrow(e);
			}
		}

		protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) {
			var rect = e.ToolStrip.ClientRectangle;
			rect.Width = e.ToolStrip.Width - e.ToolStrip.DisplayRectangle.Width - 2;
			var element = VisualStyleElement.CreateElement("menu", 13, 0);
			if (VisualStyleRenderer.IsSupported && VisualStyleRenderer.IsElementDefined(element)) {
				var renderer = new VisualStyleRenderer(element);
				renderer.DrawBackground(e.Graphics, rect);
			} else {
				base.OnRenderImageMargin(e);
			}
		}

		protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e) {
			var rect = e.Item.ContentRectangle;
			rect.Inflate(1, 1);
			rect.X += 2;
			rect.Width = rect.Height;
			var element = VisualStyleElement.CreateElement("menu", 12, !e.Item.Enabled ? 1 : 2);
			if (VisualStyleRenderer.IsSupported && VisualStyleRenderer.IsElementDefined(element)) {
				var renderer = new VisualStyleRenderer(element);
				renderer.DrawBackground(e.Graphics, rect);
			}
			// FIXME: This should cope with radios, if we can have them. Add 2 to state.
			element = VisualStyleElement.CreateElement("menu", 11, e.Item.Enabled ? 1 : 2);
			if (VisualStyleRenderer.IsSupported && VisualStyleRenderer.IsElementDefined(element)) {
				var renderer = new VisualStyleRenderer(element);
				renderer.DrawBackground(e.Graphics, rect);
			} else {
				base.OnRenderItemCheck(e);
			}
		}

		protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) {
			var element = VisualStyleElement.CreateElement("menu", !e.Item.IsOnDropDown ? 8 : 14, (e.Item.Enabled ? 0 : !e.Item.IsOnDropDown ? 3 : 2) + (!e.Item.Pressed || e.Item.IsOnDropDown ? !e.Item.Selected ? 1 : 2 : 3));
			if (VisualStyleRenderer.IsSupported && VisualStyleRenderer.IsElementDefined(element)) {
				var renderer = new VisualStyleRenderer(element);
				e.TextColor = renderer.GetColor(ColorProperty.TextColor);
				base.OnRenderItemText(e);
			} else {
				base.OnRenderItemText(e);
			}
		}

		protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e) {
			var rect = !e.Item.IsOnDropDown ? new Rectangle(new Point(), e.Item.Bounds.Size) : e.Item.ContentRectangle;
			if (e.Item.IsOnDropDown) rect.Inflate(0, 1);
			var element = VisualStyleElement.CreateElement("menu", !e.Item.IsOnDropDown ? 8 : 14, (e.Item.Enabled ? 0 : !e.Item.IsOnDropDown ? 3 : 2) + (!e.Item.Pressed || e.Item.IsOnDropDown ? !e.Item.Selected ? 1 : 2 : 3));
			if (VisualStyleRenderer.IsSupported && VisualStyleRenderer.IsElementDefined(element)) {
				var renderer = new VisualStyleRenderer(element);
				if (!e.Item.IsOnDropDown) {
					rect.Height--;
				} else {
					rect.X++;
					rect.Width--;
				}
				renderer.DrawBackground(e.Graphics, rect);
			} else {
				base.OnRenderMenuItemBackground(e);
			}
		}

		protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e) {
			var rect = e.Item.Bounds;
			rect.X = e.ToolStrip.DisplayRectangle.X - 4;
			rect.Y = 0;
			rect.Width = e.ToolStrip.DisplayRectangle.Width + 1;
			var element = VisualStyleElement.CreateElement("menu", 15, 0);
			if (VisualStyleRenderer.IsSupported && VisualStyleRenderer.IsElementDefined(element)) {
				var renderer = new VisualStyleRenderer(element);
				rect.X++;
				rect.Width -= 2;
				renderer.DrawBackground(e.Graphics, rect);
			} else {
				base.OnRenderSeparator(e);
			}
		}

		protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) {
			var rect = e.ToolStrip.ClientRectangle;
			var element = VisualStyleElement.CreateElement("menu", !e.ToolStrip.IsDropDown ? 7 : 9, 0);
			if (VisualStyleRenderer.IsSupported && VisualStyleRenderer.IsElementDefined(element)) {
				var renderer = new VisualStyleRenderer(element);
				renderer.DrawBackground(e.Graphics, rect, e.AffectedBounds);
			} else {
				base.OnRenderToolStripBackground(e);
			}
		}

		protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) {
			var rect = e.ToolStrip.ClientRectangle;
			if (e.ToolStrip.IsDropDown) {
				var element = VisualStyleElement.CreateElement("menu", 10, 0);
				if (VisualStyleRenderer.IsSupported && VisualStyleRenderer.IsElementDefined(element)) {
					var renderer = new VisualStyleRenderer(element);
					var clip = renderer.GetBackgroundContentRectangle(e.Graphics, e.ToolStrip.ClientRectangle);
					var oldClip = e.Graphics.Clip;
					e.Graphics.ExcludeClip(clip);
					renderer.DrawBackground(e.Graphics, rect, e.AffectedBounds);
					e.Graphics.Clip = oldClip;
				} else {
					base.OnRenderToolStripBorder(e);
				}
			}
		}
	}
}
