from mcp.server.fastmcp import FastMCP, Context
import logging
from dataclasses import dataclass
from contextlib import asynccontextmanager
from typing import AsyncIterator, Dict, Any, List
from config import config
from unity_connection import get_unity_connection, UnityConnection
from tools import register_all_tools

# Configure logging using settings from config
logging.basicConfig(
    level=getattr(logging, config.log_level),
    format=config.log_format
)
logger = logging.getLogger("unity-mcp-server")

# Global connection state
_unity_connection: UnityConnection | None = None

@asynccontextmanager
async def server_lifespan(server: FastMCP) -> AsyncIterator[Dict[str, Any]]:
    """Handle server startup and shutdown."""
    global _unity_connection
    logger.info("Unity MCP Server starting up")
    try:
        _unity_connection = get_unity_connection()
        logger.info("Connected to Unity on startup")
    except Exception as e:
        logger.warning(f"Could not connect to Unity on startup: {str(e)}")
        _unity_connection = None
    try:
        # Yield the connection object so it can be attached to the context
        yield {"bridge": _unity_connection}
    finally:
        if _unity_connection:
            _unity_connection.disconnect()
            _unity_connection = None
        logger.info("Unity MCP Server shut down")

# Initialize MCP server
mcp = FastMCP(
    "unity-mcp-server",
    description="Unity Editor integration via Model Context Protocol",
    lifespan=server_lifespan
)

# Register all tools
register_all_tools(mcp)

# Basic ping test tool
@mcp.tool()
def test_unity_connection(ctx: Context) -> Dict[str, Any]:
    """Test the connection to Unity Editor with a simple ping."""
    try:
        # Get Unity connection from context
        bridge = getattr(ctx, 'bridge', None) or get_unity_connection()
        
        if bridge is None:
            return {
                "success": False,
                "message": "No Unity connection available"
            }
        
        # Send ping command
        result = bridge.send_command("ping")
        
        return {
            "success": True,
            "message": "Unity connection test successful",
            "data": result
        }
    except Exception as e:
        logger.error(f"Unity connection test failed: {str(e)}")
        return {
            "success": False,
            "message": f"Unity connection test failed: {str(e)}"
        }

# Asset Creation Strategy
@mcp.prompt()
def asset_creation_strategy() -> str:
    """Guide for discovering and using Unity MCP tools effectively."""
    return (
        "Available Unity MCP Server Tools:\\n\\n"
        "- `test_unity_connection`: Test connection to Unity Editor\\n"
        "- `manage_editor`: Controls editor state and queries info.\\n"
        "- `execute_menu_item`: Executes Unity Editor menu items by path.\\n"
        "- `read_console`: Reads or clears Unity console messages, with filtering options.\\n"
        "- `manage_scene`: Manages scenes.\\n"
        "- `manage_gameobject`: Manages GameObjects in the scene.\\n"
        "- `manage_script`: Manages C# script files.\\n"
        "- `manage_asset`: Manages prefabs and assets.\\n\\n"
        "Tips:\\n"
        "- Use test_unity_connection first to verify Unity Editor connection\\n"
        "- Create prefabs for reusable GameObjects.\\n"
        "- Always include a camera and main light in your scenes.\\n"
    )

# Run the server
if __name__ == "__main__":
    mcp.run(transport='stdio')
