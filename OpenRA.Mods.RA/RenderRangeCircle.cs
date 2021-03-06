﻿#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class RenderRangeCircleInfo : TraitInfo<RenderRangeCircle> { }
	class RenderRangeCircle : IPreRenderSelection
	{
		public void RenderBeforeWorld(Actor self)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;
			
			self.World.WorldRenderer.DrawRangeCircle(
				Color.FromArgb(128, Color.Yellow),
				self.CenterLocation, (int)self.Trait<AttackBase>().GetMaximumRange());
		}
	}
}
