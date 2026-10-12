namespace CriMw.CriGears.GaPack;

public interface IGaBirth
{
	GaIndividual Birth();

	GaIndividual Birth(GaIndividual papa, GaIndividual mama);
}
