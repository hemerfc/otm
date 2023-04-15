mod imp;

use futures::{/* channel::mpsc::Sender , */ channel::mpsc::Receiver, StreamExt};

use glib::{clone, Object};
use gtk::subclass::prelude::*;
use gtk::{gio, glib, Application, NoSelection, SignalListItemFactory};
use gtk::{prelude::*, ListItem};

use crate::client_object::ClientObject;
use crate::client_row::ClientRow;
use crate::ptl_server::{/* MessageFromGtk,*/ MessageFromPtl, MessageTypeFromPtl};
//use crate::display_object::DisplayObject;
//use crate::display_item::DisplayItem;

// ANCHOR: glib_wrapper
glib::wrapper! {
    pub struct Window(ObjectSubclass<imp::Window>)
        @extends gtk::ApplicationWindow, gtk::Window, gtk::Widget,
        @implements gio::ActionGroup, gio::ActionMap, gtk::Accessible, gtk::Buildable,
                    gtk::ConstraintTarget, gtk::Native, gtk::Root, gtk::ShortcutManager;
}
// ANCHOR_END: glib_wrapper

impl Window {
    pub fn new(app: &Application) -> Self {
        // Create new window
        Object::builder::<Window>()
            .property("application", app)
            .build()
    }

    // ANCHOR: clients
    fn clients(&self) -> gio::ListStore {
        // Get state
        self.imp()
            .clients
            .borrow()
            .clone()
            .expect("Could not get current clients.")
    }

    fn setup_clients(&self) {
        // Create new model
        let model = gio::ListStore::new(ClientObject::static_type());

        // Get state and set model
        self.imp().clients.replace(Some(model));

        // Wrap model with selection and pass it to the list view
        let selection_model = NoSelection::new(Some(self.clients()));
        self.imp().client_list.set_model(Some(&selection_model));
    }
    // ANCHOR_END: clients

    // ANCHOR: setup_callbacks
    fn setup_callbacks(&self) {
        // Setup callback for activation of the entry
        self.imp()
            .entry
            .connect_activate(clone!(@weak self as window => move |_| {
                window.new_client();
            }));

        // Setup callback for clicking (and the releasing) the icon of the entry
        self.imp()
            .entry
            .connect_icon_release(clone!(@weak self as window => move |_,_| {
                window.new_client();
            }));
    }
    // ANCHOR_END: setup_callbacks

    // ANCHOR: new_client
    fn new_client(&self) {
        // Get content from entry and clear it
        let buffer = self.imp().entry.buffer();
        let content = buffer.text().to_string();
        if content.is_empty() {
            return;
        }
        buffer.set_text("");

        // Add new client to model
        let client = ClientObject::new(false, content.parse::<i32>().unwrap());
        self.clients().append(&client);
    }
    // ANCHOR_END: new_client

    // ANCHOR: new_client_with_content
    fn new_client_with_content(&self, client_id : i32) {
        // Add new client to model
        let client = ClientObject::new(false, client_id);
        self.clients().append(&client);
    }
    // ANCHOR_END: new_client_with_content

    // ANCHOR: setup_factory
    fn setup_factory(&self) {
        // Create a new factory
        let factory = SignalListItemFactory::new();

        // Create an empty `ClientRow` during setup
        factory.connect_setup(move |_, list_item| {
            // Create `ClientRow`
            let client_row = ClientRow::new();
            list_item
                .downcast_ref::<ListItem>()
                .expect("Needs to be ListItem")
                .set_child(Some(&client_row));
        });

        // Tell factory how to bind `ClientRow` to a `ClientObject`
        factory.connect_bind(move |_, list_item| {
            // Get `ClientObject` from `ListItem`
            let client_object = list_item
                .downcast_ref::<ListItem>()
                .expect("Needs to be ListItem")
                .item()
                .and_downcast::<ClientObject>()
                .expect("The item has to be an `ClientObject`.");

            // Get `ClientRow` from `ListItem`
            let client_row = list_item
                .downcast_ref::<ListItem>()
                .expect("Needs to be ListItem")
                .child()
                .and_downcast::<ClientRow>()
                .expect("The child has to be a `ClientRow`.");

            client_row.bind(&client_object);
        });

        // Tell factory how to unbind `ClientRow` from `ClientObject`
        factory.connect_unbind(move |_, list_item| {
            // Get `ClientRow` from `ListItem`
            let client_row = list_item
                .downcast_ref::<ListItem>()
                .expect("Needs to be ListItem")
                .child()
                .and_downcast::<ClientRow>()
                .expect("The child has to be a `ClientRow`.");

            client_row.unbind();
        });

        // Set the factory of the list view
        self.imp().client_list.set_factory(Some(&factory));
    }
    // ANCHOR_END: setup_factory

    // ANCHOR: spawn_local_handler
    /// Spawn channel receive client on the main event loop.
    pub fn spawn_local_handler(&self, window: Window, mut rx_ptl: Receiver<MessageFromPtl>) {
        let main_context = glib::MainContext::default();
        let future = async move {
            while let Some(msg) = rx_ptl.next().await {
                let client_id = msg.client_id;
                let content = match msg.content {
                    Some(content) => content,
                    None => String::from(""),
                };

                println!("Thread received data: {}", content);

                match msg.msg_type {
                    MessageTypeFromPtl::ClientConnected => {
                        window.new_client_with_content(client_id);
                    }
                    MessageTypeFromPtl::ClientDisconnected => {
                        todo!();
                    }
                    MessageTypeFromPtl::Confirm => {
                        todo!();
                    }
                    MessageTypeFromPtl::Read => {
                        todo!();
                    }
                    MessageTypeFromPtl::Echo => {
                        todo!();
                    }
                }
            }
        };
        main_context.spawn_local(future);
    }

    // ANCHOR_END spawn_local_handler
}
