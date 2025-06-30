"""
Defines the manage_asset tool for asset management in Unity.
"""
from typing import Dict, Any, Optional
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection

def register_manage_asset_tools(mcp: FastMCP):
    """Registers the manage_asset tool with the MCP server."""

    @mcp.tool()
    async def manage_asset(
        ctx: Context,
        action: str,
        path: str,
        asset_type: Optional[str] = None,
        properties: Optional[Dict[str, Any]] = None,
        destination: Optional[str] = None,
        search_pattern: Optional[str] = None,
    ) -> Dict[str, Any]:
        """Performs asset operations (import, create, modify, delete, etc.) in Unity.

        Args:
            ctx: The MCP context.
            action: Operation (e.g., 'import', 'create', 'search').
            path: Asset path or search scope.
            asset_type: Type for 'create' action (e.g., 'Material', 'Folder').
            properties: Properties for 'create' or 'modify'.
            destination: Target path for 'move' or 'duplicate'.
            search_pattern: Search pattern (e.g., '*.prefab').

        Returns:
            A dictionary with operation results ('success', 'data', 'error').
        """
        bridge = get_unity_connection()

        params_dict = {
            "action": action.lower(),
            "path": path,
            "assetType": asset_type,
            "properties": properties,
            "destination": destination,
            "searchPattern": search_pattern,
        }

        params_dict = {k: v for k, v in params_dict.items() if v is not None}

        # Note: Even though this tool is async, the underlying connection is sync.
        # This is a placeholder for a future fully async implementation.
        return bridge.send_command("manage_asset", params_dict) 
