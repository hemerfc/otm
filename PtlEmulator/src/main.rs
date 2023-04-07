mod client_object;
mod client_row;
mod display_object;
mod display_item;
mod window;
mod ptl_server;

use std::thread;
use futures::channel::mpsc::{ Sender, channel };
use gtk::prelude::*;
use gtk::{gio, glib, Application};
use ptl_server::{MessageFromPtl, MessageFromGtk, PtlServer};
use window::Window;

use crate::ptl_server::MessageTypeFromPtl;

// ANCHOR: main
fn main() -> glib::ExitCode {
    // Register and include resources
    gio::resources_register_include!("compiled.gresource")
        .expect("Failed to register resources.");

    // Create a new application
    let app = Application::builder()
        .application_id("org.gtk_rs.PtlEmulator")
        .build();

    // Connect to "activate" signal of `app`
    app.connect_activate(build_ui);

    // Run the application
    app.run()
}

fn build_ui(app: &Application) {
    // Create a new custom window and show it
    let window = Window::new(app);

    let (_tx_gtk, rx_gtk) = channel::<MessageFromGtk>(1000); // GTK -> PTL
    let (tx_ptl, rx_ptl) = channel::<MessageFromPtl>(1000); // PTL -> GTK
    window.spawn_local_handler(window.to_owned(), rx_ptl);

    let _server:PtlServer = PtlServer::start("127.0.0.1:25565".to_string(), tx_ptl, rx_gtk);

    window.present();
}
