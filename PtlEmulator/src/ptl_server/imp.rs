use std::io::Read;
use std::net::{TcpListener /*, TcpStream*/};
use futures::channel::mpsc::{ Sender, Receiver };
use std::sync::{mpsc, Arc, Mutex};
use std::{thread};

use crate::ptl_server::MessageTypeFromPtl;

use super::{MessageFromGtk, PtlServer, MessageFromPtl, Client};

// Implement the TCP Server struct
impl PtlServer {

    pub(crate) fn start(address: String, tx_ptl: Sender<MessageFromPtl>, _rx_gtk: Receiver<MessageFromGtk>) -> PtlServer {
        let mut client_count:usize = 0;
        let listener = TcpListener::bind(address).unwrap();
        let listener = Arc::new(Mutex::new(listener));

        let server = PtlServer {
            listener: listener,
            connected_clients: Vec::new()
        };

        let (tx, rx) = mpsc::channel();
        tx.send(server.listener.clone()).unwrap();

        thread::spawn(move || {
            let listener = rx.recv().unwrap();
            // For each new connection start a new thread
            for stream in listener.lock().unwrap().incoming() {
                let mut tx_ptl = tx_ptl.clone();
                thread::spawn(move || -> ! {
                    client_count = client_count + 1;
                    println!("New Connection! id {}", client_count);

                    let mut client = Client{
                        id : client_count,
                        stream: stream.unwrap()
                    };

                    // TODO: Add client to the connected_clients Vec
                    //server.connected_clients.push(client);

                    let mut buffer = [0; 1024];
                    loop {
                        let size = client.stream.read(&mut buffer).unwrap();
                        let mut received: Vec<u8> = vec![];
                        received.extend_from_slice(&buffer[..size]);
                        let msg = String::from_utf8(received).unwrap();
                        
                        tx_ptl.try_send(MessageFromPtl{
                            msg_type: MessageTypeFromPtl::Echo,
                            msg_data: msg
                        }).unwrap();
                    }
                });
            }
        });

        server
    }
}
