# Answer harness

Runs NovaSaur's real answer pipeline — retrieval, grounding, local answers,
prompt building — on a desktop, against 100+ questions phrased the way people
actually type them (typos included). Every question must resolve instantly
from the encyclopedia/knowledge base; anything that would need the on-device
model, or gets blocked as off-topic, fails the run.

    cd tools/AnswerHarness
    dotnet run

The project compiles the app's source files directly (see the .csproj links),
so it always tests exactly what ships. This harness is how "stuck on
thinking" bugs get caught before they ever reach a phone.
