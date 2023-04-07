use std::net::{TcpListener, TcpStream};
mod imp;

use std::sync::{ Arc, Mutex};

pub struct PtlServer {
    listener: Arc<Mutex<TcpListener>>,
    connected_clients: Vec<Client>,
    //rx_gtk: Receiver<MessageFromGtk>, 
    //tx_ptl: Sender<MessageFromPtl>,    
}

#[derive(Debug)]
pub struct Client {
    id: usize,
    stream: TcpStream,
}

pub enum MessageTypeFromPtl {
    Read,
    Confirm,
    Echo,
}

pub enum MessageTypeFromGtk {
    TurnOnDisplay,
    TurnOffDisplay
}

// define a message struct to send messages to the server
pub struct MessageFromPtl {
    pub msg_type: MessageTypeFromPtl,
    pub msg_data: String,
}

pub struct MessageFromGtk {
    pub msg_type: MessageTypeFromGtk,
    pub msg_data: String,
}