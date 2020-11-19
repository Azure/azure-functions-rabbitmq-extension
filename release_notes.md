### Release notes

- Made prefetchCount configurable and fixed ACK logic.
- Fixed RabbitMQ data type conversions for out-of-proc languages.
- **Breaking** Removed ability to create non-existent queue. Queues need to be created outside the context of the extension.
- Other fixes around warnings, builds, etc.