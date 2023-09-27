import json
import asyncio
import websockets

from Bot import Bot

bot = None

sample_size = 100
percentile = 70
last_threshold = -999
sticky_action_value = 5


def get_response(event):
    global bot

    event_info = json.loads(event)
    event_type = event_info.pop("eventType")

    if event_type == "configure":
        number_of_actions = event_info.get("numberActions")
        input_variables = event_info.get("inputVariables")

        bot = Bot(
            number_of_actions,
            sample_size,
            percentile,
            last_threshold,
            input_variables,
            sticky_action_value,
        )

        return json.dumps(
            {
                "status": "ok",
            }
        )
    elif event_type == "get_input":
        if bot is None:
            return json.dumps(
                {
                    "status": "error",
                    "message": "Bot not configured",
                }
            )

        action = bot.get_input(event_info["envData"])

        return json.dumps(
            {
                "status": "ok",
                "action": int(action),
            }
        )
    elif event_type == "set_reward":
        if bot is None:
            return json.dumps(
                {
                    "status": "error",
                    "message": "Bot not configured",
                }
            )

        bot.set_reward(event_info["reward"], event_info["done"])
        return json.dumps(
            {
                "status": "ok",
            }
        )
    else:
        return "unknown event_type"


async def server(websocket, path):
    print("Conexión establecida")
    while True:
        try:
            event = await websocket.recv()
            response = get_response(event)
            await websocket.send(response)
        except websockets.exceptions.ConnectionClosedOK:
            print("Conexión cerrada correctamente")


host = "127.0.0.1"
port = 12345
start_server = websockets.serve(server, host, port)
print(f"Listen in: {host}:{port}")

asyncio.get_event_loop().run_until_complete(start_server)
asyncio.get_event_loop().run_forever()
