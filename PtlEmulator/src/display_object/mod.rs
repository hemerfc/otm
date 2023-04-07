mod imp;

use glib::Object;
use gtk::glib;

// ANCHOR: glib_wrapper_and_new
glib::wrapper! {
    pub struct DisplayObject(ObjectSubclass<imp::DisplayObject>);
}

impl DisplayObject {
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
pub struct DisplayData {
    pub completed: bool,
    pub content: String,
}
// ANCHOR: client_data