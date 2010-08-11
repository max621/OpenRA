﻿using System.Linq;
using OpenRA.Traits;
using System;

namespace OpenRA.Mods.RA
{
	class HackyAIInfo : TraitInfo<HackyAI> { }

	/* a pile of hacks, which control the local player on the host. */

	class HackyAI : IGameStarted, ITick
	{
		bool enabled;
		int ticks;
		Player p;
		ProductionQueue pq;
		PlayerResources pr;
		int2 baseCenter;

		bool isBuildingStuff;

		enum BuildState
		{
			ChooseItem,
			WaitForProduction,
			WaitForFeedback,
		}

		int lastThinkTick = 0;

		BuildState state = BuildState.WaitForFeedback;

		public void GameStarted(World w)
		{
			p = Game.world.LocalPlayer;
			enabled = Game.IsHost && p != null;
			if (enabled)
			{
				pq = p.PlayerActor.traits.Get<ProductionQueue>();
				pr = p.PlayerActor.traits.Get<PlayerResources>();
			}
		}

		int GetPowerProvidedBy(string building)
		{
			var bi = Rules.Info[building].Traits.GetOrDefault<BuildingInfo>();
			if (bi == null) return 0;
			return bi.Power;
		}

		string ChooseItemToBuild()
		{
			var buildableThings = Rules.TechTree.BuildableItems(p, "Building").ToArray();

			if (pr.PowerProvided <= pr.PowerDrained * 1.2)	/* try to maintain 20% excess power */
			{
				/* find the best thing we can build which produces power */
				var best = buildableThings.Where(a => GetPowerProvidedBy(a) > 0)
					.OrderByDescending(a => GetPowerProvidedBy(a)).FirstOrDefault();

				if (best != null)
				{
					Game.Debug("AI: Need more power, so {0} is best choice.".F(best));
					return best;
				}
				else
					Game.Debug("AI: Need more power, but can't build anything that produces it.");
			}

			else
				Game.Debug("AI: Building some other random bits.");

			return "powr";		// LOTS OF POWER
		}

		int2? ChooseBuildLocation(ProductionItem item)
		{
			var bi = Rules.Info[ item.Item ].Traits.Get<BuildingInfo>();

			for( var i = -10; i < 10; i++ )		// fail distribution!
				for (var j = -10; j < 10; j++)
				{
					var topleft = baseCenter + new int2(i,j);
					if (Game.world.CanPlaceBuilding(item.Item, bi, topleft, null))
						if (Game.world.IsCloseEnoughToBase(p, item.Item, bi, topleft))
							return topleft;
				}

			return null;		// i don't know where to put it.
		}

		const int feedbackTime = 30;		// ticks; = a bit over 1s. must be >= netlag.

		public void Tick(Actor self)
		{
			if (!enabled)
				return;

			ticks++;

			if (ticks == 10)
			{
				/* find our mcv and deploy it */
				var mcv = self.World.Queries.OwnedBy[p]
					.FirstOrDefault(a => a.Info == Rules.Info["mcv"]);

				if (mcv != null)
				{
					baseCenter = mcv.Location;
					Game.IssueOrder(new Order("DeployTransform", mcv));
				}
				else
					Game.Debug("AI: Can't find the MCV.");
			}

			var currentBuilding = pq.CurrentItem("Building");
			switch (state)
			{
				case BuildState.ChooseItem:
					{
						var item = ChooseItemToBuild();
						if (item == null)
						{
							state = BuildState.WaitForFeedback;
							lastThinkTick = ticks;
						}
						else
						{
							state = BuildState.WaitForProduction;
							Game.IssueOrder(Order.StartProduction(p, item, 1));
						}
					}
					break;

				case BuildState.WaitForProduction:
					if (currentBuilding == null) return;	/* let it happen.. */

					else if (currentBuilding.Paused)
						Game.IssueOrder(Order.PauseProduction(p, currentBuilding.Item, false));
					else if (currentBuilding.Done)
					{
						state = BuildState.WaitForFeedback;
						lastThinkTick = ticks;

						/* place the building */
						var location = ChooseBuildLocation(currentBuilding);
						if (location == null)
						{
							Game.Debug("AI: Nowhere to place {0}".F(currentBuilding.Item));
							Game.IssueOrder(Order.CancelProduction(p, currentBuilding.Item));
						}
						else
						{
							Game.IssueOrder(new Order("PlaceBuilding", p.PlayerActor, location.Value, currentBuilding.Item));
						}
					}
					break;

				case BuildState.WaitForFeedback:
					if (ticks - lastThinkTick > feedbackTime)
						state = BuildState.ChooseItem;
					break;
			}
		}
	}
}