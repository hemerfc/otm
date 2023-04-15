mod imp;

use glib::Object;
use gtk::glib;

// ANCHOR: glib_wrapper_and_new
glib::wrapper! {
    pub struct ClientObject(ObjectSubclass<imp::ClientObject>);
}

impl ClientObject {
    pub fn new(completed: bool, client_id : i32) -> Self {
        Object::builder()
            .property("completed", completed)
            .property("client_id", client_id)
            .build()
    }
}
// ANCHOR_END: glib_wrapper_and_new

// ANCHOR: client_data
#[derive(Default)]
pub struct ClientData {
    pub completed: bool,
    pub client_id: i32,
}
// ANCHOR: client_data
