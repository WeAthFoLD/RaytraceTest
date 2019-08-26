
public static class MathUtils {

    public static float Unit2NDC(float x) {
        return (x + 1f) * 0.5f;
    }

    public static float NDC2Unit(float x) {
        return (x * 0.5f) + 0.5f;
    }

}
