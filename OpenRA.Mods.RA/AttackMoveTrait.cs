﻿using System;
using System.Drawing;
using OpenRA.Traits;
using OpenRA.Effects;

namespace OpenRA.Mods.RA
{
	class AttackMoveInfo : TraitInfo<AttackMove>
	{
		//public object Create(ActorInitializer init) { return new AttackMove(init.self); }
		public readonly bool JustMove = false;
	}

	class AttackMove : ITick, IResolveOrder, IOrderVoice
	{
		public bool AttackMoving { get; set; }

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == "AttackMove")
			{
				return "AttackMove";
			}
			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "AttackMove")
			{
				self.CancelActivity();
				//if we are just moving, we don't turn on attackmove and this becomes a regular move order
				if (!self.Info.Traits.Get<AttackMoveInfo>().JustMove)
				{
					AttackMoving = true;
				}
				Order newOrder = new Order("Move", order.Subject, order.TargetLocation);
				self.Trait<Mobile>().ResolveOrder(self, newOrder);

				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						if (order.TargetActor != null)
							w.Add(new FlashTarget(order.TargetActor));

						var line = self.TraitOrDefault<DrawLineToTarget>();
						if (line != null)
							if (order.TargetActor != null) line.SetTarget(self, Target.FromOrder(order), Color.Red);
							else line.SetTarget(self, Target.FromOrder(order), Color.Red);
					});

				if (self.Owner == self.Owner.World.LocalPlayer)
					self.World.CancelInputMode();
			}
			else
			{
				AttackMoving = false; //cancel attack move state for other orders
			}
		}

		public void Tick(Actor self)
		{
			if (self.Info.Traits.Get<AttackMoveInfo>().JustMove) return;
			if (!self.HasTrait<AttackBase>())
			{
				Game.Debug("AttackMove: {0} has no AttackBase trait".F(self.ToString()));
				return;
			}
			if (!self.IsIdle && (self.HasTrait<AttackMove>() && !(self.Trait<AttackMove>().AttackMoving))) return;

			self.Trait<AttackBase>().ScanAndAttack(self, true);
		}
	}
}
