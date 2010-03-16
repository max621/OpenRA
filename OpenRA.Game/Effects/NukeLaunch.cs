﻿#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.GameRules;
using OpenRA.Graphics;

namespace OpenRA.Effects
{
	class NukeLaunch : IEffect
	{
		readonly ProjectileInfo projectileUp, projectileDown;
		readonly Actor silo;
		Animation anim;
		float2 pos;
		int2 targetLocation;
		readonly int targetAltitude = 400;
		int altitude;
		bool goingUp = true;
		WeaponInfo weapon;

		public NukeLaunch(Actor silo, string weapon, int2 targetLocation)
		{
			this.silo = silo;
			this.targetLocation = targetLocation;
			this.weapon = Rules.WeaponInfo[weapon];
			projectileUp = Rules.ProjectileInfo["NukeUp"];
			projectileDown = Rules.ProjectileInfo["NukeDown"];

			anim = new Animation(projectileUp.Image);
			anim.PlayRepeating("idle");
			pos = silo.CenterLocation;
		}

		public void Tick(World world)
		{
			anim.Tick();

			if (goingUp)
			{
				altitude += 10;
				if (altitude >= targetAltitude)
				{
					pos = OpenRA.Traits.Util.CenterOfCell(targetLocation);
					anim = new Animation(projectileDown.Image);
					anim.PlayRepeating("idle");
					goingUp = false;
				}
			}
			else
			{
				altitude -= 10;
				if (altitude <= 0)
					Explode(world);
			}
		}

		void Explode(World world)
		{
			world.AddFrameEndTask(w => w.Remove(this));
			var warhead = Rules.WarheadInfo[weapon.Warhead];
			Combat.DoImpact(pos.ToInt2(), pos.ToInt2(), weapon, Rules.ProjectileInfo[weapon.Projectile], warhead, silo, true);
			world.WorldActor.traits.Get<ScreenShaker>().AddEffect(20, pos, 5);
		}

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, pos - 0.5f * anim.Image.size - new float2(0, altitude), "effect");
		}
	}
}