# sho
# import websocket
# ws = websocket.WebSocket()
# ws.connect("ws://echo.websocket.events")
# ws.send("Hello, Server")
# print(ws.recv())
# ws.close()

import websocket
def on_message(wsapp, message):
    print(message)
wsapp = websocket.WebSocketApp("wss://testnet.binance.vision/ws/btcusdt@trade", on_message=on_message)
wsapp.run_forever() 