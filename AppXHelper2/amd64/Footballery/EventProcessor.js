var addHandler = function(element, event, handler){
    element.addEventListener(event, handler, false);
	// TODO:  Do we need stuff for mouse?  I am thinking no.
	/*
    if (USE_MOUSE)
    {
        var mouseEvent = event.replace("MSTouch", "mouse");
        element.addEventListener(mouseEvent.toLowerCase(), handler, false);
    }
	*/
}

eventProcessor = function(evt) {
	assert(evt !== undefined, "evt is undefined!");
	assert(evt !== null, "evt is null!");
	assert(evt.type === "MSTouchDown", "unexpected event type!");

	// Prevent touch messages being promoted to gestures
	evt.preventDefault();
	
	var point = {x: evt.clientX, y: evt.clientY};
	var divisiveXCoord = canvasWidth/2;

	// BUG:  181505
	updater.processTouchUpdate(point);
	/*
	if (connectionType === "Server"){
		if (point.x > divisiveXCoord){
			updater.processTouchUpdate(point);
		}
		else{
			return;
		}
	}
	else if (connectionType === "Client"){
		if (point.x < divisiveXCoord){
			updater.processTouchUpdate(point);
		}
		else{
			return;
		}
	}
	else{
		// if connectionType not set, then we ignore everything
		return;
	}
	*/
}
