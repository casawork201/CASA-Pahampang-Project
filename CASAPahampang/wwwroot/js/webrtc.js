let dotNetHelper;
let localStream;
let currentRoom = null;
const peerConnections = {};

const rtcConfig = {
    iceServers: [
        { urls: 'stun:stun.l.google.com:19302' },
        { urls: 'stun:stun1.l.google.com:19302' }
    ]
};

window.initWebRTC = function (dotnetRef) {
    dotNetHelper = dotnetRef;
    console.log("[WebRTC JS] Module initialized successfully! 🚀");
};

// 💡 Join a specific room (e.g., 'basketball' or 'volleyball')
window.joinGameRoom = async function (roomName) {
    currentRoom = roomName;
    await dotNetHelper.invokeMethodAsync('JoinRoom', roomName);
    console.log(`[WebRTC JS] Successfully joined room: ${roomName} 🏟️`);
};

window.startBroadcast = async function (roomName, userName) {
    try {
        currentRoom = roomName;
        await dotNetHelper.invokeMethodAsync('JoinRoom', roomName);

        if (localStream) {
            localStream.getTracks().forEach(track => track.stop());
            localStream = null;
        }

        // 📱 Mobile-friendly constraints using the front camera by default
        const constraints = {
            video: {
                facingMode: 'user',
                width: { ideal: 1280 },
                height: { ideal: 720 }
            },
            audio: {
                echoCancellation: true,
                noiseSuppression: true
            }
        };

        localStream = await navigator.mediaDevices.getUserMedia(constraints);
        const localVideo = document.getElementById('localVideo');
        if (localVideo) {
            localVideo.srcObject = localStream;
            await localVideo.play().catch(err => console.log("Local video error:", err));
        }

        await dotNetHelper.invokeMethodAsync('BroadcastWebcamStarted', roomName, userName);
        console.log(`[WebRTC JS] Broadcast started in room '${roomName}' and notification sent! 🎥✨`);
    } catch (err) {
        console.error("[WebRTC JS] Camera error:", err);
        throw err;
    }
};

window.connectToBroadcaster = async function (broadcasterId) {
    const pc = createPeerConnection(broadcasterId, true);
    try {
        const offer = await pc.createOffer();
        await pc.setLocalDescription(offer);
        await dotNetHelper.invokeMethodAsync('SendSignal', broadcasterId, 'offer', JSON.stringify(offer));
        console.log("[WebRTC JS] Sent offer to broadcaster:", broadcasterId);
    } catch (err) {
        console.error("[WebRTC JS] Offer creation error:", err);
    }
};

window.stopBroadcast = async function () {
    if (currentRoom) {
        await dotNetHelper.invokeMethodAsync('BroadcastWebcamStopped', currentRoom);
        await dotNetHelper.invokeMethodAsync('LeaveRoom', currentRoom);
    }

    if (localStream) {
        localStream.getTracks().forEach(track => {
            track.stop();
            console.log(`[WebRTC JS] Stopped local track: ${track.kind}`);
        });
        localStream = null;
        await new Promise(resolve => setTimeout(resolve, 300));
    }

    const localVideo = document.getElementById('localVideo');
    if (localVideo) {
        localVideo.srcObject = null;
    }

    for (const peerId in peerConnections) {
        const pc = peerConnections[peerId];
        if (pc) {
            pc.close();
            console.log(`[WebRTC JS] Closed peer connection for peer: ${peerId}`);
        }
    }
    
    Object.keys(peerConnections).forEach(key => delete peerConnections[key]);
    currentRoom = null;
    console.log("[WebRTC JS] Broadcast successfully stopped and all resources cleaned up! 🛑✨");
};

function createPeerConnection(peerId, isViewer) {
    if (peerConnections[peerId]) {
        return peerConnections[peerId];
    }

    console.log("[WebRTC JS] Creating isolated RTCPeerConnection for peer:", peerId);
    const pc = new RTCPeerConnection(rtcConfig);
    
    pc.pendingCandidates = [];
    peerConnections[peerId] = pc;

    if (isViewer) {
        pc.addTransceiver('video', { direction: 'recvonly' });
        pc.addTransceiver('audio', { direction: 'recvonly' });
    }

    pc.onicecandidate = (event) => {
        if (event.candidate) {
            dotNetHelper.invokeMethodAsync('SendSignal', peerId, 'candidate', JSON.stringify(event.candidate));
        }
    };

    pc.ontrack = (event) => {
        const remoteVideo = document.getElementById('remoteVideo');
        if (remoteVideo && event.streams[0]) {
            remoteVideo.srcObject = event.streams[0];
            remoteVideo.muted = true;
            remoteVideo.play().catch(e => console.log("Playback error:", e));
            console.log("[WebRTC JS] Remote stream bound and playing successfully! 📺🎉");
        }
    };

    pc.onconnectionstatechange = () => {
        if (pc.connectionState === 'disconnected' || pc.connectionState === 'failed' || pc.connectionState === 'closed') {
            delete peerConnections[peerId];
        }
    };

    return pc;
}

async function setRemoteDescriptionSafely(pc, description) {
    await pc.setRemoteDescription(description);
    if (pc.pendingCandidates && pc.pendingCandidates.length > 0) {
        console.log(`[WebRTC JS] Flushing ${pc.pendingCandidates.length} queued ICE candidate(s)... ⚡`);
        for (const candidate of pc.pendingCandidates) {
            try {
                await pc.addIceCandidate(candidate);
            } catch (err) {
                console.error("[WebRTC JS] Error adding queued ICE candidate:", err);
            }
        }
        pc.pendingCandidates = [];
    }
}

window.handleSignalingData = async function (senderId, type, payload) {
    console.log(`[WebRTC JS] 📥 Received signal type '${type}' from sender: ${senderId}`);
    
    let pc = peerConnections[senderId];
    if (!pc) {
        pc = createPeerConnection(senderId, false);
    }

    const data = JSON.parse(payload);
    try {
        if (type === 'offer') {
            console.log("[WebRTC JS] 📝 Setting remote description for incoming offer...");
            await setRemoteDescriptionSafely(pc, new RTCSessionDescription(data));
            
            if (localStream) {
                localStream.getTracks().forEach(track => {
                    console.log("[WebRTC JS] ➕ Adding local track to peer connection:", track.kind);
                    pc.addTrack(track, localStream);
                });
            }

            console.log("[WebRTC JS] 💡 Creating answer...");
            const answer = await pc.createAnswer();
            await pc.setLocalDescription(answer);
            await dotNetHelper.invokeMethodAsync('SendSignal', senderId, 'answer', JSON.stringify(answer));
            console.log("[WebRTC JS] 🚀 Answer created and sent back!");
            
        } else if (type === 'answer') {
            console.log("[WebRTC JS] 📝 Setting remote description for incoming answer...");
            await setRemoteDescriptionSafely(pc, new RTCSessionDescription(data));
            console.log("[WebRTC JS] 🎉 Answer remote description applied successfully!");
            
        } else if (type === 'candidate') {
            const candidate = new RTCIceCandidate(data);
            if (pc.remoteDescription && pc.remoteDescription.type) {
                await pc.addIceCandidate(candidate);
                console.log("[WebRTC JS] ✅ ICE candidate added immediately.");
            } else {
                console.log("[WebRTC JS] ⏳ Remote description not set yet. Queuing ICE candidate... 📦");
                pc.pendingCandidates.push(candidate);
            }
        }
    } catch (err) {
        console.error(`[WebRTC JS] ❌ Error handling signaling data (${type}):`, err);
    }
};

window.clearRemoteVideo = async function() {
    const remoteVideo = document.getElementById('remoteVideo');
    if (remoteVideo) {
        remoteVideo.srcObject = null;
    }
    
    for (const peerId in peerConnections) {
        const pc = peerConnections[peerId];
        if (pc) {
            pc.close();
            console.log(`[WebRTC JS] Closed peer connection for broadcaster: ${peerId}`);
        }
    }
    Object.keys(peerConnections).forEach(key => delete peerConnections[key]);
    console.log("[WebRTC JS] Viewer stream and connections cleared. Standing by... ⏳");
};