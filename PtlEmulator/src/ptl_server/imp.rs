use futures::channel::mpsc::{Receiver, Sender};
use std::io::Read;
use std::net::TcpListener;
use std::sync::{mpsc, Arc, Mutex};
use std::thread;

use crate::ptl_server::MessageTypeFromPtl;

use super::{ MessageFromGtk, MessageFromPtl, PtlServer};

// Implement the TCP Server struct
impl PtlServer {
    pub(crate) fn start(
        address: String,
        tx_ptl: Sender<MessageFromPtl>,
        _rx_gtk: Receiver<MessageFromGtk>,
    ) {
        let mut client_count: usize = 0;
        let listener = TcpListener::bind(address).unwrap();
        let listener = Arc::new(Mutex::new(listener));

        let (tx, rx) = mpsc::channel();
        tx.send(listener.clone()).unwrap();

        /*
        let mut _server = PtlServer {
            listener: listener,
            connected_clients: Vec::new(),
        };
        */

        thread::spawn(move || {
            let listener = rx.recv().unwrap();

            // For each new connection start a new thread
            for stream in listener.lock().unwrap().incoming() {
                client_count += 1;

                println!("New Connection! id {}", client_count);

                create_client_thread(&tx_ptl, client_count, stream);

                // TODO: Add client to the connected_clients Vec
                // server.connected_clients.push(client);
            }
        });
    }
}

fn create_client_thread(
    tx_ptl: &Sender<MessageFromPtl>,
    client_id: usize,
    stream: Result<std::net::TcpStream, std::io::Error>,
) {
    let mut tx_ptl = tx_ptl.clone();

    //let client = Client { _id: client_id };
    //let _client = Arc::new(Mutex::new(client));

    thread::spawn(move || {
        let mut buffer = [0; 1024];
        let mut stream = stream.unwrap();
        let mut read_counter = 0;
        loop {
            let res = stream.read(&mut buffer);
            read_counter += 1;

            match res {
                Ok(size) => {
                    println!("Received {} bytes from client {} read_conter {} ", size, client_id, read_counter);

                    if size > 0 {
                        let mut received: Vec<u8> = vec![];
                        received.extend_from_slice(&buffer[..size]);
                        //let msg = String::from_utf8(received).unwrap();

                        let _res = tx_ptl.try_send(MessageFromPtl {
                            msg_type: MessageTypeFromPtl::ClientConnected,
                            client_id: client_id as i32,
                            content: None,
                        });

                        match _res {
                            Ok(_) => {
                                println!("Sent message to PTL");
                            }
                            Err(_) => {
                                println!("Failed to send message to PTL");
                            }
                        }
                    } else {
                        println!("Client {} disconnected read_conter {} ", client_id, read_counter);
                        
                        let _res = tx_ptl.try_send(MessageFromPtl {
                            msg_type: MessageTypeFromPtl::ClientDisconnected,
                            client_id: client_id as i32,
                            content: None,
                        });
                        
                        break;
                    }
                }
                Err(_) => {
                    println!("Client {} disconnected read_conter {} ", client_id, read_counter);
                }
            }
        }
    });
}
