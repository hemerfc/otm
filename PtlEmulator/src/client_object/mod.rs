mod imp;

use glib::Object;
use gtk::glib;

// ANCHOR: glib_wrapper_and_new
glib::wrapper! {
    pub struct ClientObject(ObjectSubclass<imp::ClientObject>);
}

impl ClientObject {
    pub fn new(completed: bool, content: String) -> Self {
        Object::builder()
            .property("completed", completed)
            .property("content", content)
            .build()
    }
}
// ANCHOR_END: glib_wrapper_and_new

// ANCHOR: client_data
#[derive(Default)]
pub struct ClientData {
    pub completed: bool,
    pub content: String,
}
// ANCHOR: client_data