use std::net::TcpListener;
mod imp;

use std::sync::{Arc, Mutex};

pub struct PtlServer {
    _listener: Arc<Mutex<TcpListener>>,
    _connected_clients: Vec<Arc<Mutex<Client>>>,
}

#[derive(Debug)]
pub struct Client {
    _id: usize,
}

pub enum MessageTypeFromPtl {
    ClientConnected,
    ClientDisconnected,
    Read,
    Confirm,
    Echo,
}

pub enum MessageTypeFromGtk {
    //TurnOnDisplay,
    //TurnOffDisplay,
}

// define a message struct to send messages to the server
pub struct MessageFromPtl {
    pub msg_type: MessageTypeFromPtl,
    pub content: Option<String>,
    pub client_id: i32,
}

pub struct MessageFromGtk {
    pub msg_type: MessageTypeFromGtk,
    pub content: Option<String>,
    pub client_id: i32,
}
