MediatR
=======

**MediatR** is a low-ambition library trying to solve a simple problem: decoupling the
in-process sending of messages from handling messages. Cross-platform, supporting
``netstandard2.0``.

MediatR has two kinds of messages it dispatches:

* Request/response messages, dispatched to a single handler
* Notification messages, dispatched to multiple handlers

New to MediatR? Check out the :doc:`Setup` page first.

.. toctree::
   :maxdepth: 2
   :caption: Overview

   Setup
   License-Key-Configuration

.. toctree::
   :maxdepth: 2
   :caption: Features

   Requests-and-responses
   Streams
   Notifications
   Behaviors
   Generic-requests-and-handlers
   Polymorphic-dispatch
   Async
   Exception-handling
   Container-Feature-Support

.. toctree::
   :maxdepth: 1
   :caption: Migration guides

   Migration-Guide-12.0-to-12.1
   Migration-Guide-11.x-to-12.0
   Migration-Guide-10.x-to-11.0
   Migration-Guide-9.x-to-10.0
   Migration-Guide-4.x-to-5.0
   Migration-Guide-3.x-to-4.0
