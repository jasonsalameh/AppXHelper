
// Server Socket and Listener
var server;
var serverListener;

// Client Socket and Operations
var client;
var clientConnectOp;

var socket;

// WriteOperation and communication port
var writeOperation;
var port = "20004";

var connected = false;

//Server Code to run
function StartServer() 
{
	server = new Windows.Networking.Sockets.DatagramSocket();
	server.addEventListener('MessageReceived', onServerMessageReceive);
	serverListener = server.ListenAsync(port);
	serverListener.Completed = onServerListenCompleted;
	serverListener.Start();

	socket = server;
}


function onServerMessageReceive(sender, e)
{
	var data = e.Data;
	var type = data.ReadString();
	var getXYObject = function (data) {
		var x = data.ReadDouble();
		assert(x !== undefined, "x is undefined");

		var y = data.ReadDouble();
		assert(y !== undefined, "y is undefined");

		return { x: x, y: y };
	};

	if(type === "BallPositionUpdate") {
		updater.processBallUpdate(getXYObject(data));
	}
	else if (type === "BallSpeedUpdate") {
		updater.processBallSpeedUpdate(getXYObject(data));
	}
}

function onServerListenCompleted(sender, e)
{

}

function CloseServer()
{
	server.Close();
}

// Client side connection code
function StartClient()
{
	client = new Windows.Networking.Sockets.DatagramSocket();
    connectOp = client.ConnectAsync(ipaddress,port);
	client.addEventListener('MessageReceived', onServerMessageReceive);
    clientConnectOp.Completed = onConnecting;
    clientConnectOp.Start();

	socket = client;
}

function onConnecting()
{
	if (clientConnectOp.ErrorCode == 0)
	{
		connected = true;
	}
}


function CloseClient()
{
	client.Close();
}

function onWriteCompleted()
{

}

function WriteOperation(socket, ip, packet) {
	if(ip) {
		writeOperation = socket.WriteAsync(packet, ip, port);
		writeOperation.Completed = onWriteCompleted;
		writeOperation.Start();
	}
}

