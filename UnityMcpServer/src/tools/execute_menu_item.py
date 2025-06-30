"""
Defines the execute_menu_item tool for executing menu items in Unity.
"""
from typing import Dict, Any
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection

def register_execute_menu_item_tools(mcp: FastMCP):
    """Registers the execute_menu_item tool with the MCP server."""

    @mcp.tool()
    def execute_menu_item(
        ctx: Context,
        menu_path: str,
    ) -> Dict[str, Any]:
        """Executes a Unity Editor menu item via its path (e.g., "File/Save Project").

        Args:
            ctx: The MCP context.
            menu_path: The full path of the menu item to execute.

        Returns:
            A dictionary indicating success or failure, with optional message/error.
        """
        bridge = get_unity_connection()

        params_dict = {
            "menuPath": menu_path,
        }

        return bridge.send_command("execute_menu_item", params_dict) 
