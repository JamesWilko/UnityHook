using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Assets.CS.TabletopUI;
using Assets.TabletopUi.Scripts.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace Hooks
{
	[RuntimeHook]
	class GameSpeedHook
	{
		const int SuperFastSpeedMultiplier = 15;

		private SpeedController speedController = null;

		public GameSpeedHook()
		{
			HookRegistry.Register( OnCall );
		}

		public static string[] GetExpectedMethods()
		{
			return new string[] {
				"Assets.TabletopUi.Scripts.Infrastructure.HotkeyWatcher::WatchForGameplayHotkeys",
				"Heart::Beat",
				"Assets.TabletopUi.Scripts.Infrastructure.SpeedController::Initialise",
				"Assets.TabletopUi.Scripts.Infrastructure.SpeedController::SetFastForward"
			};
		}

		object OnCall( string typeName, string methodName, object thisObj, object[] args, IntPtr[] refArgs, int[] refIdxMatch )
		{
			if ( typeName == "Assets.TabletopUi.Scripts.Infrastructure.HotkeyWatcher" && methodName == "WatchForGameplayHotkeys" )
			{
				if( Input.GetKeyDown( KeyCode.Comma ) )
				{
					SetFastForward();
					SetSuperFastSpeed();
				}
			}

			if( typeName == "Heart" && methodName == "Beat" )
			{
				OnCallHeartBeat( thisObj, args, refArgs, refIdxMatch );
			}

			if( typeName == "Assets.TabletopUi.Scripts.Infrastructure.SpeedController" && methodName == "Initialise" )
			{
				OnCallSpeedControllerInit( thisObj, args, refArgs, refIdxMatch );
			}

			if( typeName == "Assets.TabletopUi.Scripts.Infrastructure.SpeedController" && methodName == "SetFastForward" )
			{
				Heart heart = (Heart) speedController.GetType().GetField( "_heart", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( speedController );
				GameSpeed CurrentGameSpeed = (GameSpeed) heart.GetType().GetField( "CurrentGameSpeed", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( heart );

				if ( CurrentGameSpeed == GameSpeed.Fast )
				{
					SetSuperFastSpeed();
				}
				else
				{
					SetFastForward();
				}
				return true;
			}

			return null;
		}

		object OnCallHeartBeat( object thisObj, object[] args, IntPtr[] refArgs, int[] refIdxMatch )
		{
			Heart heart = (Heart) thisObj;
			float usualInterval = (float) heart.GetType().GetField( "usualInterval", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( heart );
			GameSpeed CurrentGameSpeed = (GameSpeed) heart.GetType().GetField( "CurrentGameSpeed", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( heart );

			float intervalThisBeat = usualInterval;
			if ( CurrentGameSpeed == GameSpeed.Fast )
			{
				intervalThisBeat = usualInterval * 3f;
			}
			if ( CurrentGameSpeed > GameSpeed.Fast )
			{
				intervalThisBeat = usualInterval * (float) CurrentGameSpeed;
			}

			heart.AdvanceTime( intervalThisBeat );

			FieldInfo beatCounterField = heart.GetType().GetField( "usualInterval", BindingFlags.NonPublic | BindingFlags.Instance );
			int beatCounter = (int) beatCounterField.GetValue( heart ) + 1;
			if ( beatCounter >= 20 )
			{
				beatCounterField.SetValue( heart, 0 );

				FieldInfo outstandingSlotsToFillField = heart.GetType().GetField( "outstandingSlotsToFill", BindingFlags.NonPublic | BindingFlags.Instance );
				HashSet<Assets.TabletopUi.TokenAndSlot> outstandingSlotsToFillValue = outstandingSlotsToFillField.GetValue( heart ) as HashSet<Assets.TabletopUi.TokenAndSlot>;
				HashSet<Assets.TabletopUi.TokenAndSlot> outstandingSlotsToFillRet = Registry.Retrieve<TabletopManager>().FillTheseSlotsWithFreeStacks( outstandingSlotsToFillValue );
				outstandingSlotsToFillField.SetValue( heart, outstandingSlotsToFillRet );
			}

			return null;
		}

		object OnCallSpeedControllerInit( object thisObj, object[] args, IntPtr[] refArgs, int[] refIdxMatch )
		{
			speedController = (SpeedController) thisObj;
			return null;
		}

		void SetFastForward()
		{
			Heart heart = (Heart) speedController.GetType().GetField( "_heart", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( speedController );
			bool isLocked = (bool) speedController.GetType().GetField( "isLocked", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( speedController );

			if ( !isLocked )
			{
				if ( heart.IsPaused )
				{
					speedController.SetPausedState( false, true );
				}
				heart.SetGameSpeed( GameSpeed.Fast );

				Button normalSpeedButton = (Button) speedController.GetType().GetField( "normalSpeedButton", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( speedController );
				Button fastForwardButton = (Button) speedController.GetType().GetField( "fastForwardButton", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( speedController );
				normalSpeedButton.GetComponent<Image>().color = Color.white;
				fastForwardButton.GetComponent<Image>().color = new Color32( 147, 225, 239, 255 );
			}
		}

		void SetSuperFastSpeed()
		{
			Heart heart = (Heart) speedController.GetType().GetField( "_heart", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( speedController );
			bool isLocked = (bool) speedController.GetType().GetField( "isLocked", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( speedController );

			if ( !isLocked )
			{
				if ( heart.IsPaused )
				{
					speedController.SetPausedState( false, true );
				}
				heart.SetGameSpeed( (GameSpeed) SuperFastSpeedMultiplier );

				Button normalSpeedButton = (Button) speedController.GetType().GetField( "normalSpeedButton", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( speedController );
				Button fastForwardButton = (Button) speedController.GetType().GetField( "fastForwardButton", BindingFlags.NonPublic | BindingFlags.Instance ).GetValue( speedController );
				normalSpeedButton.GetComponent<Image>().color = Color.white;
				fastForwardButton.GetComponent<Image>().color = new Color32( 239, 147, 171, 255 );
			}
		}

	}
}
