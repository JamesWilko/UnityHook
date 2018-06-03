using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Assets.CS.TabletopUI;
using Assets.TabletopUi.Scripts.Infrastructure;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hooks
{
	[RuntimeHook]
	class SnapToGridHook
	{
		const float ElementGridSizeX = 40.0f;
		const float ElementGridSizeY = 60.0f;
		const float SituationGridSizeX = 50.0f;
		const float SituationGridSizeY = 50.0f;

		public SnapToGridHook()
		{
			HookRegistry.Register( OnCall );
		}

		public static string[] GetExpectedMethods()
		{
			return new string[] {
				"Assets.TabletopUi.Scripts.Infrastructure.Choreographer::GetFreePosWithDebug",
				"Assets.CS.TabletopUI.DraggableToken::DelayedEndDrag",
				"Assets.TabletopUi.Scripts.Infrastructure.HotkeyWatcher::WatchForGameplayHotkeys",
			};
		}

		Vector2 GetGridSize( DraggableToken token )
		{
			if( token is ElementStackToken )
			{
				return new Vector2( ElementGridSizeX, ElementGridSizeY );
			}
			else
			{
				return new Vector2( SituationGridSizeX, SituationGridSizeY );
			}
		}

		object OnCall( string typeName, string methodName, object thisObj, object[] args, IntPtr[] refArgs, int[] refIdxMatch )
		{
			bool snapToGrid = !Input.GetKey( KeyCode.LeftControl ) && !Input.GetKey( KeyCode.RightControl );

			if ( typeName == "Assets.TabletopUi.Scripts.Infrastructure.Choreographer" && methodName == "GetFreePosWithDebug" )
			{
				if ( snapToGrid )
				{
					Choreographer choreo = (Choreographer) thisObj;
					MethodInfo getFreeTokenPositionMethod = choreo.GetType().GetMethod( "GetFreeTokenPosition", BindingFlags.NonPublic | BindingFlags.Instance );
					Vector2 pos = (Vector2) getFreeTokenPositionMethod.Invoke( choreo, args );
					Vector2 gridSize = GetGridSize( (DraggableToken) args[0] );
					pos.x = gridSize.x * ( Mathf.Round( pos.x / gridSize.x ) );
					pos.y = gridSize.y * ( Mathf.Round( pos.y / gridSize.y ) );
					return pos;
				}
			}

			if( typeName == "Assets.CS.TabletopUI.DraggableToken" && methodName == "DelayedEndDrag" )
			{
				if ( snapToGrid )
				{
					DraggableToken token = (DraggableToken) thisObj;
					Vector3 pos = token.RectTransform.localPosition;
					Vector2 gridSize = GetGridSize( token );
					pos.x = gridSize.x * ( Mathf.Round( pos.x / gridSize.x ) );
					pos.y = gridSize.y * ( Mathf.Round( pos.y / gridSize.y ) );
					token.RectTransform.localPosition = pos;
				}
			}

			if ( typeName == "Assets.TabletopUi.Scripts.Infrastructure.HotkeyWatcher" && methodName == "WatchForGameplayHotkeys" )
			{
				if ( Input.GetKeyDown( KeyCode.A ) )
				{
					TabletopTokenContainer ttc = Registry.Retrieve<TabletopManager>()._tabletop;
					foreach ( DraggableToken token in ttc.GetTokens() )
					{
						Vector3 pos = token.transform.localPosition;
						Vector2 gridSize = GetGridSize( token );
						pos.x = gridSize.x * ( Mathf.Round( pos.x / gridSize.x ) );
						pos.y = gridSize.y * ( Mathf.Round( pos.y / gridSize.y ) );
						token.RectTransform.localPosition = pos;
					}
				}

			}

			return null;
		}

	}
}
